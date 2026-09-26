using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using SliceDetails.Data;

namespace SliceDetails
{
	internal class SliceRecorder : UnityEngine.Object, IInitializable, IDisposable
	{
		private readonly BeatmapObjectManager _beatmapObjectManager;
		private readonly SliceProcessor _sliceProcessor;
		private readonly ScoreController _scoreController;

		private Dictionary<NoteData, NoteInfo> _noteSwingInfos = new Dictionary<NoteData, NoteInfo>();
		private List<NoteInfo> _noteInfos = new List<NoteInfo>();
		private int _lastProcessedCount = -1;

		public SliceRecorder(BeatmapObjectManager beatmapObjectManager, ScoreController scoreController, SliceProcessor sliceProcessor) {
			_beatmapObjectManager = beatmapObjectManager;
			_scoreController = scoreController;
			_sliceProcessor = sliceProcessor;
		}

		public void Initialize() {
			_beatmapObjectManager.noteWasCutEvent += OnNoteWasCut;
			_scoreController.scoringForNoteFinishedEvent += ScoringForNoteFinishedHandler;
			_sliceProcessor.ResetProcessor();
		}

		public void Dispose() {
			_beatmapObjectManager.noteWasCutEvent -= OnNoteWasCut;
			_scoreController.scoringForNoteFinishedEvent -= ScoringForNoteFinishedHandler;
			// Process slices once the map ends
			ProcessSlices();
			_noteSwingInfos.Clear();
			_noteInfos.Clear();
		}

		public void ClearSlices() {
			_noteInfos.Clear();
			_noteSwingInfos.Clear();
			_lastProcessedCount = -1;
			ProcessSlices();
		}

		public void ProcessSlices() {
			if (_sliceProcessor.ready && _lastProcessedCount == _noteInfos.Count) return;
			_sliceProcessor.ProcessSlices(_noteInfos);
			_lastProcessedCount = _noteInfos.Count;
		}

		private void OnNoteWasCut(NoteController noteController, in NoteCutInfo noteCutInfo) {
			if (noteController.noteData.colorType == ColorType.None || !noteCutInfo.allIsOK) return;
			ProcessNote(noteController, noteCutInfo);
		}

		private void ProcessNote(NoteController noteController, in NoteCutInfo noteCutInfo) {
			if (noteController == null) return;
			var noteData = noteController.noteData;
			if (_noteSwingInfos.ContainsKey(noteData)) return;
			if (noteData.scoringType != NoteData.ScoringType.Normal &&
				noteData.scoringType != NoteData.ScoringType.ArcHead &&
				noteData.scoringType != NoteData.ScoringType.ArcTail &&
				noteData.scoringType != NoteData.ScoringType.ChainHead) return;
			
			Vector2 noteGridPosition;
			noteGridPosition.y = (int)noteController.noteData.noteLineLayer;
			noteGridPosition.x = noteController.noteData.lineIndex;
			int noteIndex = (int)(noteGridPosition.y * 4 + noteGridPosition.x);

			// No ME notes allowed >:(
			if (noteGridPosition.x >= 4 || noteGridPosition.y >= 3 || noteGridPosition.x < 0 || noteGridPosition.y < 0) return;

			Vector2 cutDirection = new Vector3(-noteCutInfo.cutNormal.y, noteCutInfo.cutNormal.x);
			float cutAngle = Mathf.Atan2(cutDirection.y, cutDirection.x) * Mathf.Rad2Deg + 180f;

			float cutOffset = noteCutInfo.cutDistanceToCenter;
			Vector3 noteCenter = noteController.noteTransform.position;
			if (Vector3.Dot(noteCutInfo.cutNormal, noteCutInfo.cutPoint - noteCenter) > 0f)
			{
				cutOffset = -cutOffset;
			}

			NoteInfo noteInfo = new NoteInfo(noteData, cutAngle, cutOffset, noteIndex);
			_noteSwingInfos.Add(noteData, noteInfo);
		}

		public void ScoringForNoteFinishedHandler(ScoringElement scoringElement) {
			NoteInfo noteSwingInfo;
			if (_noteSwingInfos.TryGetValue(scoringElement.noteData, out noteSwingInfo))
			{
				_noteSwingInfos.Remove(scoringElement.noteData);
				if (!(scoringElement is GoodCutScoringElement goodScoringElement)) return;

				IReadonlyCutScoreBuffer cutScoreBuffer = goodScoringElement.cutScoreBuffer;

				int preSwing = cutScoreBuffer.beforeCutScore;
				int postSwing = cutScoreBuffer.afterCutScore;
				int offset = cutScoreBuffer.centerDistanceCutScore;

				switch (goodScoringElement.noteData.scoringType)
				{
					case NoteData.ScoringType.Normal:
						noteSwingInfo.score = new Score(preSwing, postSwing, offset);
						_noteInfos.Add(noteSwingInfo);
						break;
					case NoteData.ScoringType.ArcHead:
						if (!Plugin.Settings.CountArcs) break;
						noteSwingInfo.score = new Score(preSwing, null, offset);
						_noteInfos.Add(noteSwingInfo);
						break;
					case NoteData.ScoringType.ArcTail:
						if (!Plugin.Settings.CountArcs) break;
						noteSwingInfo.score = new Score(null, postSwing, offset);
						_noteInfos.Add(noteSwingInfo);
						break;
					case NoteData.ScoringType.ChainHead:
						if (!Plugin.Settings.CountChains) break;
						noteSwingInfo.score = new Score(preSwing, null, offset);
						_noteInfos.Add(noteSwingInfo);
						break;
				}

			}
			else {
				// Bad cut, do nothing
			}
		}
	}
}
