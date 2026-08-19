using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using HMS_Chess.Engine.Modes;
using HMS_Chess.Engine.Models;
using HMS_Chess.Engine.Pieces;

namespace HMS_Chess
{
    public partial class MainWindow : Window
    {
        private Board board;
        private PieceColor currentTurn = PieceColor.White;

        // Interaction Tracking States
        private Position? selectedPosition = null;
        private List<Position> highlightedMoves = new List<Position>();

        // Visual Tracking States
        private Position? lastMoveFrom = null;
        private Position? lastMoveTo = null;
        private Position? checkedKingPosition = null;

        // Pending promotion tracking state
        private Position? promotionPendingPosition = null;

        // Custom local path for the HMS asset directory structure
        private readonly string assetsPath = @"C:\00 Projects\C#\HMS Chess\Assets\set01";

        public MainWindow()
        {
            InitializeComponent();
            board = new Board();
            InitializeUserInterface();
        }
        private void InitializeUserInterface()
        {
            ChessBoardGrid.Children.Clear();

            // Scan current check parameters using our GameEngine
            checkedKingPosition = GameEngine.IsKingInCheck(board, currentTurn)
                ? GameEngine.FindKingPosition(board, currentTurn)
                : null;

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    // Invert rendering row index so White sits correctly at the bottom row frame
                    int displayRow = 7 - row;
                    Position pos = new Position(displayRow, col);
                    Piece? piece = board.GetPieceAt(pos);

                    // Dynamic Background Assignment handles highlights BEFORE drawing children
                    Grid cellContainer = new Grid { Background = GetSquareVisualBrush(pos) };

                    // LAYER 1: Render Graphical Pieces (Draws flat on top of the Grid background color)
                    if (piece != null)
                    {
                        Image pieceImage = new Image
                        {
                            Source = GetPieceImageSource(piece),
                            Margin = new Thickness(2),
                            IsHitTestVisible = false // Prevents transparency layers from trapping mouse updates
                        };
                        RenderOptions.SetBitmapScalingMode(pieceImage, BitmapScalingMode.HighQuality);
                        cellContainer.Children.Add(pieceImage);
                    }

                    // LAYER 2: Lichess Move Indicator Overlays (Dots and Targets)
                    if (highlightedMoves.Any(m => m.Equals(pos)))
                    {
                        if (piece == null)
                        {
                            // Empty Target square -> Center Dot indicator
                            Ellipse dot = new Ellipse
                            {
                                Width = 20,
                                Height = 20,
                                Fill = new SolidColorBrush(Color.FromArgb(65, 0, 0, 0)),
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                IsHitTestVisible = false
                            };
                            cellContainer.Children.Add(dot);
                        }
                        else
                        {
                            // Occupied Enemy target square -> Target Frame Ring Border outline
                            Ellipse ring = new Ellipse
                            {
                                Margin = new Thickness(2),
                                Stroke = new SolidColorBrush(Color.FromArgb(75, 0, 0, 0)),
                                StrokeThickness = 5,
                                IsHitTestVisible = false
                            };
                            cellContainer.Children.Add(ring);
                        }
                    }

                    // LAYER 3: Transparent Interaction Action Button Overlay
                    Button actionOverlay = new Button
                    {
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Tag = pos
                    };
                    actionOverlay.Click += Square_Click;
                    cellContainer.Children.Add(actionOverlay);

                    ChessBoardGrid.Children.Add(cellContainer);
                }
            }
        }

        // Consolidates state prioritizations straight to the underlying tile pixel brush
        private Brush GetSquareVisualBrush(Position pos)
        {
            if (checkedKingPosition != null && pos.Equals(checkedKingPosition))
            {
                return new SolidColorBrush(Color.FromRgb(217, 59, 59)); // Matte Crimson
            }

            if (selectedPosition != null && pos.Equals(selectedPosition))
            {
                return new SolidColorBrush(Color.FromRgb(247, 247, 105)); // Lichess Matte Yellow
            }

            if (pos.Equals(lastMoveFrom) || pos.Equals(lastMoveTo))
            {
                return new SolidColorBrush(Color.FromRgb(186, 196, 73)); // Khaki Olive
            }

            return (pos.Row + pos.Column) % 2 == 0
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f0d9b5"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#b58863"));
        }
        private void Square_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton && clickedButton.Tag is Position clickedPos)
            {
                if (highlightedMoves.Any(m => m.Equals(clickedPos)) && selectedPosition != null)
                {
                    board.ExecuteMove(selectedPosition, clickedPos);

                    lastMoveFrom = selectedPosition;
                    lastMoveTo = clickedPos;

                    Piece? movedPiece = board.GetPieceAt(clickedPos);
                    if (movedPiece is Pawn && (clickedPos.Row == 7 || clickedPos.Row == 0))
                    {
                        // Pause normal turn switching loops and launch selection panel overlay instead
                        promotionPendingPosition = clickedPos;
                        ShowPromotionModal(movedPiece.Color);
                        ClearSelectionStates();
                        return;
                    }

                    AdvanceTurnSequence();
                }
                else
                {
                    Piece? piece = board.GetPieceAt(clickedPos);
                    if (piece != null && piece.Color == currentTurn)
                    {
                        selectedPosition = clickedPos;
                        highlightedMoves = GameEngine.GetAbsoluteLegalMoves(board, piece);
                    }
                    else
                    {
                        ClearSelectionStates();
                    }
                }
                InitializeUserInterface();
            }
        }

        private void AdvanceTurnSequence()
        {
            currentTurn = (currentTurn == PieceColor.White) ? PieceColor.Black : PieceColor.White;
            ClearSelectionStates();
            CheckEndGameConditions();
        }
                private void ShowPromotionModal(PieceColor color)
        {
            PromotionOptionsPanel.Children.Clear();
            string[] piecesToPromote = { "Q", "R", "B", "N" };

            foreach (string type in piecesToPromote)
            {
                Button optionButton = new Button
                {
                    Width = 80, Height = 80,
                    Margin = new Thickness(5),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#363532")),
                    BorderThickness = new Thickness(0),
                    Tag = type
                };

                string colorLetter = color == PieceColor.White ? "w" : "b";
                string fullPath = System.IO.Path.Combine(assetsPath, $"{colorLetter}{type}.png");
                
                if (File.Exists(fullPath))
                {
                    Image img = new Image { Source = new BitmapImage(new Uri(fullPath, UriKind.Absolute)) };
                    RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                    optionButton.Content = img;
                }

                optionButton.Click += PromotionOption_Click;
                PromotionOptionsPanel.Children.Add(optionButton);
            }

            PromotionOverlay.Visibility = Visibility.Visible;
        }

        private void PromotionOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string choice && promotionPendingPosition != null)
            {
                PieceColor color = board.GetPieceAt(promotionPendingPosition)?.Color ?? currentTurn;
                Piece newPiece = choice switch
                {
                    "R" => new Rook(color, promotionPendingPosition),
                    "B" => new Bishop(color, promotionPendingPosition),
                    "N" => new Knight(color, promotionPendingPosition),
                    _   => new Queen(color, promotionPendingPosition)
                };

                board.SetPieceAt(promotionPendingPosition, newPiece);
                
                PromotionOverlay.Visibility = Visibility.Collapsed;
                promotionPendingPosition = null;

                AdvanceTurnSequence();
                InitializeUserInterface();
            }
        }

        private void CheckEndGameConditions()
        {
            if (GameEngine.IsCheckmate(board, currentTurn))
            {
                MessageBox.Show($"Checkmate! Game Over. Winner: {(currentTurn == PieceColor.White ? "Black" : "White")}", "HMS Chess Notification", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (GameEngine.IsStalemate(board, currentTurn))
            {
                MessageBox.Show("Stalemate! The match ends in a draw.", "HMS Chess Notification", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClearSelectionStates()
        {
            selectedPosition = null;
            highlightedMoves.Clear();
        }

        private ImageSource? GetPieceImageSource(Piece piece)
        {
            string colorLetter = piece.Color == PieceColor.White ? "w" : "b";
            string initial = piece switch
            {
                King => "K", Queen => "Q", Rook => "R",
                Bishop => "B", Knight => "N", Pawn => "P",
                _ => string.Empty
            };

            string fileName = $"{colorLetter}{initial}.png";
            string fullPath = System.IO.Path.Combine(assetsPath, fileName);

            if (File.Exists(fullPath)) return new BitmapImage(new Uri(fullPath, UriKind.Absolute));
            return null;
        }
    }
}


