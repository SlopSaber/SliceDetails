using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SliceDetails.Data
{
	internal class NoteInfo
	{
		public NoteCutDirection cutDirection;
		public ColorType colorType;
		public float cutAngle;
		public float cutOffset;
		public Score score;
		public int noteIndex;

		public NoteInfo() { 
			
		}

		public NoteInfo(NoteData noteData, float cutAngle, float cutOffset, int noteIndex) {
			cutDirection = noteData.cutDirection;
			colorType = noteData.colorType;
			this.cutAngle = cutAngle;
			this.cutOffset = cutOffset;
			this.noteIndex = noteIndex;
		}
	}
}
