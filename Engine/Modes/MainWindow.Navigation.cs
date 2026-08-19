using HMS_Chess.Engine.Models;
using HMS_Chess.Engine.Modes;
using HMS_Chess.Engine.Pieces;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HMS_Chess
{
    public partial class MainWindow
    {
        private List<BoardHistorySnapshot> historyTimeline = new List<BoardHistorySnapshot>();
        private int currentMoveIndex = -1;
        private bool isBrowsingHistory = false;

        private ICommand? _moveFirstCommand;
        private ICommand? _movePreviousCommand;
        private ICommand? _moveNextCommand;
        private ICommand? _moveLastCommand;

        private void InitializeNavigationSystem()
        {
            this.DataContext = this;
        }

        #region Navigation Commands (Bound to UI Elements)

        public ICommand MoveFirstCommand => _moveFirstCommand ??= new RelayCommand(
            execute: _ => JumpToHistoryFrame(-1),
            canExecute: _ => historyTimeline.Count > 0 && currentMoveIndex > -1
        );

        public ICommand MovePreviousCommand => _movePreviousCommand ??= new RelayCommand(
            execute: _ => JumpToHistoryFrame(currentMoveIndex - 1),
            canExecute: _ => currentMoveIndex > -1
        );

        public ICommand MoveNextCommand => _moveNextCommand ??= new RelayCommand(
            execute: _ => JumpToHistoryFrame(currentMoveIndex + 1),
            canExecute: _ => currentMoveIndex < historyTimeline.Count - 1
        );

        public ICommand MoveLastCommand => _moveLastCommand ??= new RelayCommand(
            execute: _ => JumpToHistoryFrame(historyTimeline.Count - 1),
            canExecute: _ => historyTimeline.Count > 0 && currentMoveIndex < historyTimeline.Count - 1
        );

        #endregion

        private void JumpToHistoryFrame(int targetIndex)
        {
            if (targetIndex < -1 || targetIndex >= historyTimeline.Count) return;

            currentMoveIndex = targetIndex;
            isBrowsingHistory = (currentMoveIndex < historyTimeline.Count - 1);

            if (currentMoveIndex == -1)
            {
                board = new Board();
                lastMoveFrom = null;
                lastMoveTo = null;
                currentTurn = PieceColor.White;
            }
            else
            {
                var targetFrame = historyTimeline[currentMoveIndex];
                board = CloneBoardState(targetFrame.SavedBoardState);
                lastMoveFrom = targetFrame.From;
                lastMoveTo = targetFrame.To;
                currentTurn = targetFrame.NextTurn;
            }

            ClearSelectionStates();
            HighlightActiveMoveInLog();
            InitializeUserInterface();
        }

        public void CaptureCurrentBoardToTimeline(Position from, Position to, PieceColor movedColor)
        {
            if (currentMoveIndex < historyTimeline.Count - 1)
            {
                historyTimeline = historyTimeline.GetRange(0, currentMoveIndex + 1);
                RebuildMoveLogFromTimeline();
            }

            PieceColor nextColor = (movedColor == PieceColor.White) ? PieceColor.Black : PieceColor.White;
            var frame = new BoardHistorySnapshot(from, to, board, nextColor);

            historyTimeline.Add(frame);
            currentMoveIndex = historyTimeline.Count - 1;
            HighlightActiveMoveInLog();
        }

        private Board CloneBoardState(Board original)
        {
            Board copy = new Board();
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Position pos = new Position(r, c);
                    Piece? piece = original.GetPieceAt(pos);
                    if (piece != null)
                    {
                        copy.SetPieceAt(pos, piece);
                    }
                }
            }
            return copy;
        }

        private void HighlightActiveMoveInLog()
        {
            int indexCounter = 0;
            foreach (var child in MoveLogStackPanel.Children)
            {
                if (child is StackPanel rowPanel)
                {
                    foreach (var innerChild in rowPanel.Children)
                    {
                        if (innerChild is TextBlock textBlock && textBlock.Tag is int timelineIndex)
                        {
                            textBlock.Foreground = (timelineIndex == currentMoveIndex)
                                ? new SolidColorBrush(Color.FromRgb(247, 247, 105))
                                : Brushes.White;
                            textBlock.FontWeight = (timelineIndex == currentMoveIndex)
                                ? System.Windows.FontWeights.Bold
                                : System.Windows.FontWeights.Medium;
                        }
                    }
                }
            }
        }

        private void RebuildMoveLogFromTimeline()
        {
            MoveLogStackPanel.Children.Clear();
            fullMoveCount = 1;

            for (int i = 0; i < historyTimeline.Count; i++)
            {
                var frame = historyTimeline[i];
                // Visual rows are programmatically appended here when rewinding and cutting moves
            }
        }
    }

    public class BoardHistorySnapshot
    {
        public Position From { get; }
        public Position To { get; }
        public Board SavedBoardState { get; }
        public PieceColor NextTurn { get; }

        public BoardHistorySnapshot(Position from, Position to, Board currentBoard, PieceColor nextTurn)
        {
            From = from;
            To = to;
            NextTurn = nextTurn;

            SavedBoardState = new Board();
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Position pos = new Position(r, c);
                    Piece? piece = currentBoard.GetPieceAt(pos);
                    if (piece != null)
                    {
                        SavedBoardState.SetPieceAt(pos, piece);
                    }
                }
            }
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
