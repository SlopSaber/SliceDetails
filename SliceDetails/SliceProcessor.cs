using SliceDetails.UI;
using SliceDetails.Data;
using System;
using System.Collections.Generic;

namespace SliceDetails
{
	internal class SliceProcessor
	{
		public Tile[] tiles = new Tile[12];
		public bool ready { get; private set; } = false;

		public void ResetProcessor() {
			ready = false;

			for (int i = 0; i < 12; i++) {
				tiles[i] ??= new Tile();
				tiles[i].Reset();
			}
		}

		public void ProcessSlices(List<NoteInfo> noteInfos) {
			ResetProcessor();

			// Populate the tiles' note infos.  Each List<NoteInfo> in tileNoteInfos cooresponds to each direction/color combination (i.e. DownLeft/ColorA)
			// where elements 0-8 are ColorA notes and elements 9-17 are ColorB notes numbering from NoteCutDirection.Up (0) to NoteCutDirection.Any (8)
			foreach (NoteInfo ni in noteInfos) {
				int noteDirection = ni.cutDirection switch {
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
				int noteColor = (int)ni.colorType;
				if (noteDirection < 0 || noteColor < 0 || noteColor > 1) continue;
				int tileNoteDataIndex = noteColor * 9 + noteDirection;

				tiles[ni.noteIndex].tileNoteInfos[tileNoteDataIndex].Add(ni);
			}

			// Calculate average angles and offsets
			for (int i = 0; i < 12; i++) {
				tiles[i].CalculateAverages();
			}

			ready = true;
		}
	}
}
