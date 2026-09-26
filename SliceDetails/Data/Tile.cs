using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SliceDetails.Data
{
	internal class Tile
	{
		public List<NoteInfo>[] tileNoteInfos = new List<NoteInfo>[18];
		public float[] angleAverages = new float[18];
		public float[] offsetAverages = new float[18];
		public Score[] scoreAverages = new Score[18];
		public int[] noteCounts = new int[18];
		public float scoreAverage = 0.0f;
		public bool atLeastOneNote = false;
		public int noteCount = 0;

		public Tile() {
			for (int i = 0; i < 18; i++) {
				tileNoteInfos[i] = new List<NoteInfo>();
				scoreAverages[i] = new Score(0f, 0f, 0f);
			}
		}

		public void Reset() {
			for (int i = 0; i < tileNoteInfos.Length; i++)
				tileNoteInfos[i].Clear();
			ClearAverages();
		}

		private void ClearAverages() {
			Array.Clear(angleAverages, 0, angleAverages.Length);
			Array.Clear(offsetAverages, 0, offsetAverages.Length);
			Array.Clear(noteCounts, 0, noteCounts.Length);
			for (int i = 0; i < scoreAverages.Length; i++) {
				scoreAverages[i].PreSwing = scoreAverages[i].PostSwing = scoreAverages[i].Offset = 0f;
				scoreAverages[i].CountPreSwing = scoreAverages[i].CountPostSwing = false;
			}
			scoreAverage = 0f;
			noteCount = 0;
			atLeastOneNote = false;
		}

		public void CalculateAverages() {
			ClearAverages();
			for (int i = 0; i < tileNoteInfos.Length; i++) {
				if (tileNoteInfos[i].Count > 0) {
					int preSwingCount = 0;
					int postSwingCount = 0;
					Vector2 angleXYAverages = Vector2.zero;
					foreach (NoteInfo noteInfo in tileNoteInfos[i]) {
						atLeastOneNote = true;
						angleXYAverages.x += Mathf.Cos(noteInfo.cutAngle * Mathf.PI / 180f);
						angleXYAverages.y += Mathf.Sin(noteInfo.cutAngle * Mathf.PI / 180f);
						offsetAverages[i] += noteInfo.cutOffset;
						scoreAverages[i].PreSwing += noteInfo.score.PreSwing;
						scoreAverages[i].PostSwing += noteInfo.score.PostSwing;
						scoreAverages[i].Offset += noteInfo.score.Offset;
						noteCounts[i]++;
						scoreAverage += noteInfo.score.TotalScore;
						preSwingCount += noteInfo.score.CountPreSwing ? 1 : 0;
						postSwingCount += noteInfo.score.CountPostSwing ? 1 : 0;
						noteCount++;
					}
					angleXYAverages.x /= tileNoteInfos[i].Count;
					angleXYAverages.y /= tileNoteInfos[i].Count;
					angleAverages[i] = Mathf.Atan2(angleXYAverages.y, angleXYAverages.x) * 180f / Mathf.PI;
					offsetAverages[i] /= tileNoteInfos[i].Count;
					if (preSwingCount > 0) scoreAverages[i].PreSwing /= preSwingCount;
					if (postSwingCount > 0) scoreAverages[i].PostSwing /= postSwingCount;
					scoreAverages[i].CountPreSwing = preSwingCount > 0;
					scoreAverages[i].CountPostSwing = postSwingCount > 0;
					scoreAverages[i].Offset /= tileNoteInfos[i].Count;
				}
			}
			if (noteCount > 0) scoreAverage /= noteCount;
		}
	}
}
