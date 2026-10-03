using System.IO;
using System.Reflection;
using System;
using System.Threading.Tasks;
using IPA.Utilities.Async;
using UnityEngine;

namespace SliceDetails.UI
{
	internal class AssetLoader : IDisposable
	{
		private static readonly Task<byte[][]> ResourceBytes = Task.Run(ReadResourceBytes);
		private bool _disposed;

		public Sprite spr_arrow { get; private set; }
		public Sprite spr_dot { get; private set; }
		public Sprite spr_roundrect { get; private set; }
		public Sprite spr_square { get; private set; }
		internal Task Ready { get; }
		internal bool IsReady { get; private set; }

		public AssetLoader() {
			Ready = UnityMainThreadTaskScheduler.Factory.StartNew(LoadSprites).Unwrap();
		}

		public void Dispose() {
			_disposed = true;
			IsReady = false;
		}

		private static byte[][] ReadResourceBytes() {
			Assembly assembly = typeof(AssetLoader).Assembly;
			string[] names = { "SliceDetails.Resources.arrow.png", "SliceDetails.Resources.dot.png", "SliceDetails.Resources.bloq.png" };
			var result = new byte[names.Length][];
			for (int i = 0; i < names.Length; i++) {
				using (Stream stream = assembly.GetManifestResourceStream(names[i])) {
					if (stream == null)
						continue;
					result[i] = new byte[stream.Length];
					stream.Read(result[i], 0, (int)stream.Length);
				}
			}
			return result;
		}

		private async Task LoadSprites() {
			byte[][] bytes = await ResourceBytes;
			if (_disposed)
				return;
			spr_arrow = LoadSpriteFromBytes(bytes[0]);
			spr_dot = LoadSpriteFromBytes(bytes[1]);
			spr_roundrect = LoadSpriteFromBytes(bytes[2]);
			var square = new Texture2D(2, 2) { filterMode = FilterMode.Point };
			square.Apply();
			spr_square = Sprite.Create(square, new Rect(0, 0, square.width, square.height), Vector2.zero, 100);
			IsReady = true;
		}

		public static Sprite LoadSpriteFromResource(string path) {
			Assembly assembly = Assembly.GetCallingAssembly();
			using (Stream stream = assembly.GetManifestResourceStream(path)) {
				if (stream != null) {
					byte[] data = new byte[stream.Length];
					stream.Read(data, 0, (int)stream.Length);
					return LoadSpriteFromBytes(data);
				}
			}
			return null;
		}

		private static Sprite LoadSpriteFromBytes(byte[] data) {
			if (data == null)
				return null;
			Texture2D tex = new Texture2D(2, 2);
			if (tex.LoadImage(data))
				return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0), 100);
			UnityEngine.Object.Destroy(tex);
			return null;
		}
	}
}
