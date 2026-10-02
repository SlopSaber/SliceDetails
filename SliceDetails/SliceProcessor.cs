using SliceDetails.UI;
using SliceDetails.Data;
using SiraUtil.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SliceDetails
{
	internal class SliceProcessor
	{
		private sealed class Snapshot
		{
			public readonly Snapshot Previous;
			public readonly NoteCutDirection Direction;
			public readonly ColorType Color;
			public readonly float Angle, Offset, PreSwing, PostSwing, ScoreOffset;
			public readonly bool CountPreSwing, CountPostSwing;
			public readonly int Index, Count;

			public Snapshot(Snapshot previous, NoteInfo note) {
				Previous = previous;
				Direction = note.cutDirection;
				Color = note.colorType;
				Angle = note.cutAngle;
				Offset = note.cutOffset;
				PreSwing = note.score.PreSwing;
				PostSwing = note.score.PostSwing;
				ScoreOffset = note.score.Offset;
				CountPreSwing = note.score.CountPreSwing;
				CountPostSwing = note.score.CountPostSwing;
				Index = note.noteIndex;
				Count = (previous?.Count ?? 0) + 1;
			}
		}

		private readonly SiraLog _siraLog;
		private Snapshot _history, _pending;
		private Task<Tile[]> _worker;
		private bool _hasPending;
		private int _pendingRevision;
		private int _workerRevision;
		public Tile[] tiles = new Tile[12];
		public bool ready { get; private set; }
		public bool processing => (_worker != null && _workerRevision == revision) || _hasPending;
		public int revision { get; private set; }

		public SliceProcessor(SiraLog siraLog) {
			_siraLog = siraLog;
		}

		public void BeginRecording() {
			_history = null;
			ResetProcessor();
		}

		public void RecordNote(NoteInfo note) {
			_history = new Snapshot(_history, note);
		}

		public void ResetProcessor() {
			++revision;
			ready = false;
			_pending = null;
			_hasPending = false;
			tiles = EmptyTiles();
		}

		public void ProcessSlices() {
			ready = false;
			_pendingRevision = ++revision;
			_pending = _history;
			_hasPending = true;
			if (_worker == null)
				StartPending();
		}

		private void StartPending() {
			Snapshot input = _pending;
			int requestRevision = _pendingRevision;
			_workerRevision = requestRevision;
			_pending = null;
			_hasPending = false;
			_worker = Task.Factory.StartNew(Prepare, input, CancellationToken.None,
				TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
			Observe(_worker, requestRevision);
		}

		private async void Observe(Task<Tile[]> worker, int requestRevision) {
			try {
				Tile[] prepared = await worker;
				if (requestRevision == revision) {
					tiles = prepared;
					ready = true;
				}
			} catch (Exception error) {
				_siraLog.Error(error);
			} finally {
				// An invalidated result owns the physical slot until its task finishes.
				if (ReferenceEquals(_worker, worker)) {
					_worker = null;
					if (_hasPending)
						StartPending();
				}
			}
		}

		private static Tile[] EmptyTiles() {
			var result = new Tile[12];
			for (int i = 0; i < result.Length; ++i)
				result[i] = new Tile();
			return result;
		}

		private static Tile[] Prepare(object state) {
			var history = (Snapshot)state;
			var ordered = new Snapshot[history?.Count ?? 0];
			for (int i = ordered.Length - 1; i >= 0; --i) {
				ordered[i] = history;
				history = history.Previous;
			}
			Tile[] result = EmptyTiles();
			foreach (Snapshot note in ordered) {
				int noteDirection = note.Direction switch {
					NoteCutDirection.UpLeft => (int)OrderedNoteCutDirection.UpLeft,
					NoteCutDirection.Up => (int)OrderedNoteCutDirection.Up,
					NoteCutDirection.UpRight => (int)OrderedNoteCutDirection.UpRight,
					NoteCutDirection.Left => (int)OrderedNoteCutDirection.Left,
					NoteCutDirection.Any => (int)OrderedNoteCutDirection.Any,
					NoteCutDirection.Right => (int)OrderedNoteCutDirection.Right,
					NoteCutDirection.DownLeft => (int)OrderedNoteCutDirection.DownLeft,
					NoteCutDirection.Down => (int)OrderedNoteCutDirection.Down,
					NoteCutDirection.DownRight => (int)OrderedNoteCutDirection.DownRight,
					_ => -1
				};
				int noteColor = (int)note.Color;
				if (noteDirection < 0 || noteColor < 0 || noteColor > 1) continue;
				int tileNoteDataIndex = noteColor * 9 + noteDirection;

				var score = new Score(note.PreSwing, note.PostSwing, note.ScoreOffset) {
					CountPreSwing = note.CountPreSwing, CountPostSwing = note.CountPostSwing
				};
				var info = new NoteInfo {
					cutDirection = note.Direction, colorType = note.Color,
					cutAngle = note.Angle, cutOffset = note.Offset,
					noteIndex = note.Index, score = score
				};
				result[note.Index].tileNoteInfos[tileNoteDataIndex].Add(info);
			}

			for (int i = 0; i < result.Length; i++)
				result[i].CalculateAverages();
			return result;
		}
	}
}
