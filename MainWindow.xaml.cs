using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Input; // Required for MouseButtonEventArgs
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
                    int displayRow = 7 - row;
                    Position pos = new Position(displayRow, col);
                    Piece? piece = board.GetPieceAt(pos);

                    // LAYER 1: Base Container (Kept transparent to isolate layers)
                    Grid cellContainer = new Grid { Background = Brushes.Transparent };

                    // LAYER 2: Core Board Square Background
                    cellContainer.Children.Add(new Rectangle
                    {
                        Fill = GetBaseSquareThemeBrush(pos),
                        IsHitTestVisible = false
                    });

                    // LAYER 3: Last Move Highlight Overlay (Solid Khaki Olive underneath pieces)
                    if (pos.Equals(lastMoveFrom) || pos.Equals(lastMoveTo))
                    {
                        cellContainer.Children.Add(new Rectangle
                        {
                            Fill = new SolidColorBrush(Color.FromRgb(186, 196, 73)),
                            IsHitTestVisible = false
                        });
                    }

                    // LAYER 4: Check Warning Highlight Overlay (Solid Crimson underneath pieces)
                    if (checkedKingPosition != null && pos.Equals(checkedKingPosition))
                    {
                        cellContainer.Children.Add(new Rectangle
                        {
                            Fill = new SolidColorBrush(Color.FromRgb(217, 59, 59)),
                            IsHitTestVisible = false
                        });
                    }

                    // LAYER 5: Active Selection Highlight Overlay (Crisp Square Border Outline)
                    if (selectedPosition != null && pos.Equals(selectedPosition))
                    {
                        cellContainer.Children.Add(new Rectangle
                        {
                            Stroke = new SolidColorBrush(Color.FromRgb(247, 247, 105)),
                            StrokeThickness = 4,
                            IsHitTestVisible = false
                        });
                    }

                    // LAYER 6: Graphical Pieces (Guaranteed on top of background fills/borders)
                    if (piece != null)
                    {
                        Image pieceImage = new Image
                        {
                            Source = GetPieceImageSource(piece),
                            Margin = new Thickness(4),
                            IsHitTestVisible = false
                        };
                        RenderOptions.SetBitmapScalingMode(pieceImage, BitmapScalingMode.HighQuality);
                        cellContainer.Children.Add(pieceImage);
                    }

                    // LAYER 7: Lichess Move Indicator Overlays (Dots and Targets)
                    if (highlightedMoves.Any(m => m.Equals(pos)))
                    {
                        if (piece == null)
                        {
                            Ellipse dot = new Ellipse
                            {
                                Width = 22,
                                Height = 22,
                                Fill = new SolidColorBrush(Color.FromArgb(65, 0, 0, 0)),
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                IsHitTestVisible = false
                            };
                            cellContainer.Children.Add(dot);
                        }
                        else
                        {
                            Ellipse ring = new Ellipse
                            {
                                Margin = new Thickness(4),
                                Stroke = new SolidColorBrush(Color.FromArgb(75, 0, 0, 0)),
                                StrokeThickness = 5,
                                IsHitTestVisible = false
                            };
                            cellContainer.Children.Add(ring);
                        }
                    }

                    // LAYER 8: Transparent Interaction Border Overlay (Bypasses OS focus styling completely)
                    Border actionOverlay = new Border
                    {
                        Background = Brushes.Transparent,
                        Tag = pos
                    };
                    actionOverlay.MouseDown += Square_MouseDown;
                    cellContainer.Children.Add(actionOverlay);

                    ChessBoardGrid.Children.Add(cellContainer);
                }
            }
        }

        private Brush GetBaseSquareThemeBrush(Position pos)
        {
            // FIXED: Changing == to != ensures a1 is dark and the bottom-right square (h1) is light
            return (pos.Row + pos.Column) % 2 != 0
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f0d9b5"))  // Solid Light sand
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#b58863")); // Solid Matte brown
        }
        private int fullMoveCount = 1;
        private string currentMoveRowString = "";

        private void Square_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Process calculations only on primary left-mouse down states
            if (e.ChangedButton == MouseButton.Left && sender is Border clickedBorder && clickedBorder.Tag is Position clickedPos)
            {
                if (highlightedMoves.Any(m => m.Equals(clickedPos)) && selectedPosition != null)
                {
                    // READ TARGET CONTEXT BEFORE THE ENGINE ALTERS DATA
                    Piece? movedPiece = board.GetPieceAt(selectedPosition);
                    Piece? capturedPiece = board.GetPieceAt(clickedPos);

                    if (movedPiece != null)
                    {
                        // Generate the proper FIDE algebraic notation code string
                        string notation = GenerateAlgebraicNotation(selectedPosition, clickedPos, movedPiece, capturedPiece);
                        AppendMoveToLogPanel(notation, movedPiece.Color);
                    }

                    // Execute the validated move on the backend grid board structure
                    board.ExecuteMove(selectedPosition, clickedPos);

                    lastMoveFrom = selectedPosition;
                    lastMoveTo = clickedPos;

                    Piece? checkPromotionPiece = board.GetPieceAt(clickedPos);
                    if (checkPromotionPiece is Pawn && (clickedPos.Row == 7 || clickedPos.Row == 0))
                    {
                        // Pause turn rotation loops and launch selection panel overlay instead
                        promotionPendingPosition = clickedPos;
                        ShowPromotionModal(checkPromotionPiece.Color);
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

        private void AppendMoveToLogPanel(string notation, PieceColor color)
        {
            if (color == PieceColor.White)
            {
                // Start a new numbering index block row string for White
                currentMoveRowString = $"{fullMoveCount}. {notation}";

                TextBlock moveRow = new TextBlock
                {
                    Text = currentMoveRowString,
                    Foreground = Brushes.White,
                    FontSize = 14,
                    Margin = new Thickness(0, 4, 0, 4),
                    FontWeight = FontWeights.Medium
                };
                MoveLogStackPanel.Children.Add(moveRow);
            }
            else
            {
                // Find and update the existing line row for Black's matching response turn
                if (MoveLogStackPanel.Children.Count > 0 && MoveLogStackPanel.Children[MoveLogStackPanel.Children.Count - 1] is TextBlock lastRow)
                {
                    lastRow.Text = $"{lastRow.Text}   {notation}";
                }
                fullMoveCount++; // Increment current move index loop tracking
            }

            // Automatically force the scroll viewer to stay tracked to the bottom item
            MoveLogScrollViewer.ScrollToEnd();
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

        // Converts coordinate actions into professional Algebraic Notation format strings
        private string GenerateAlgebraicNotation(Position from, Position to, Piece movedPiece, Piece? capturedPiece)
        {
            // 1. Handle Kingside vs Queenside Castling detection patterns
            if (movedPiece is King && Math.Abs(to.Column - from.Column) == 2)
            {
                return (to.Column > from.Column) ? "O-O" : "O-O-O";
            }

            string moveString = string.Empty;

            // 2. Add prefix tracking for pieces (Pawns remain headless empty identifiers unless capturing)
            if (movedPiece is not Pawn)
            {
                moveString += GetPieceAbbreviation(movedPiece);
            }

            // 3. Append capturing delimiters ('x') according to official rules
            if (capturedPiece != null || (movedPiece is Pawn && from.Column != to.Column))
            {
                if (movedPiece is Pawn)
                {
                    // Pawns prefix their native starting file letter when capturing (e.g., exd5)
                    moveString += (char)('a' + from.Column);
                }
                moveString += "x";
            }

            // 4. Map the targeting grid cell coordinates
            char file = (char)('a' + to.Column);
            int rank = to.Row + 1;
            moveString += $"{file}{rank}";

            return moveString;
        }

        private string GetPieceAbbreviation(Piece piece)
        {
            return piece switch
            {
                King => "K",
                Queen => "Q",
                Rook => "R",
                Bishop => "B",
                Knight => "N",
                Pawn => "P",
                _ => string.Empty
            };
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

