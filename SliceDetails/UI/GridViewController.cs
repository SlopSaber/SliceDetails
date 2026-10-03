using System;
using System.Collections.Generic;
using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using IPA.Utilities;
using SiraUtil.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;
using SliceDetails.Data;
using UnityEngine.SceneManagement;

namespace SliceDetails.UI
{
	public enum OrderedNoteCutDirection
	{
		UpLeft = 0,
		Up = 1,
		UpRight = 2,
		Left = 3,
		Any = 4,
		Right = 5,
		DownLeft = 6,
		Down = 7,
		DownRight = 8,
		None = 9
	}

	[HotReload(RelativePathToLayout = @"Views\gridView.bsml")]
	[ViewDefinition("SliceDetails.UI.Views.gridView.bsml")]
	internal class GridViewController : BSMLAutomaticViewController {
		private static readonly FieldInfo ContentsField = typeof(BSMLViewController).GetField("contentObject", BindingFlags.Instance | BindingFlags.NonPublic);

		private SiraLog _siraLog;
		private AssetLoader _assetLoader;
		private HoverHintControllerHandler _hoverHintControllerHandler;
		private SliceProcessor _sliceProcessor;
		private DiContainer _diContainer;

		[UIObject("tile-grid")]
		private GameObject _tileGrid { get; set; }
		[UIObject("tile-row")]
		private GameObject _tileRow { get; set; }
		[UIComponent("tile")]
		private ClickableImage _tile { get; set; }

		[UIObject("note-modal")]
		private GameObject _noteModal { get; set; }
		[UIObject("note-horizontal")]
		private GameObject _noteHorizontal { get; set; }
		[UIObject("note-grid")]
		private GameObject _noteGrid { get; set; }
		[UIObject("note-row")]
		private GameObject _noteRow { get; set; }

		[UIComponent("note")]
		private ImageView _note { get; set; }
		[UIComponent("note-dir-arrow")]
		private ImageView _noteDirArrow { get; set; }
		[UIComponent("note-cut-arrow")]
		private ImageView _noteCutArrow { get; set; }
		[UIComponent("note-cut-distance")]
		private ImageView _noteCutDistance { get; set; }
		[UIComponent("sd-version")]
		private TextMeshProUGUI _sdVersionText { get; set; }
		[UIComponent("reset-button")]
		private RectTransform _resetButtonTransform { get; set; }

		private List<ClickableImage> _tiles = new List<ClickableImage>();
		private List<NoteUI> _notes = new List<NoteUI>();
		private SelectedTileIndicator _selectedTileIndicator;
		private BasicUIAudioManager _basicUIAudioManager;
		private bool _refreshPending;
		private int _refreshRevision;
		private int _parseRevision;
		private bool _gridReady;


		[Inject]
		internal void Construct(SiraLog siraLog, AssetLoader assetLoader, HoverHintControllerHandler hoverHintControllerHandler, SliceProcessor sliceProcessor, DiContainer diContainer) {
			_siraLog = siraLog;
			_assetLoader = assetLoader;
			_hoverHintControllerHandler = hoverHintControllerHandler;
			_sliceProcessor = sliceProcessor;
			_diContainer = diContainer;
			_siraLog.Debug("GridViewController Constructed");
		}

		[UIAction("#post-parse")]
		public async void PostParse() {
			int revision = ++_parseRevision;
			_gridReady = false;
			AssetLoader assetLoader = _assetLoader;
			GameObject contents = (GameObject)ContentsField.GetValue(this);
			GameObject tileGrid = _tileGrid;
			ImageView note = _note;
			try {
				await assetLoader.Ready;
				if (!IsCurrentParse(revision, assetLoader, contents) || !assetLoader.IsReady || tileGrid == null || tileGrid != _tileGrid || note == null || note != _note)
					return;
				BuildGrid();
				if (!IsCurrentParse(revision, assetLoader, contents))
					return;
				_gridReady = true;
				SetTileScores();
			} catch (Exception exception) {
				if (IsCurrentParse(revision, assetLoader, contents))
					ShowFallback(exception);
			}
		}

		private bool IsCurrentParse(int revision, AssetLoader assetLoader, GameObject contents) {
			return this && revision == _parseRevision && assetLoader == _assetLoader && contents != null && contents == (GameObject)ContentsField.GetValue(this);
		}

		private void ShowFallback(Exception exception) {
			_gridReady = false;
			++_parseRevision;
			foreach (ClickableImage tile in _tiles) {
				if (tile != null)
					tile.OnClickEvent -= SetNotesData;
			}
			_tiles.Clear();
			_notes.Clear();
			_selectedTileIndicator = null;
			_siraLog.Error(exception);
			try {
				ClearContents();
				GameObject contents = (GameObject)ContentsField.GetValue(this);
				BSMLParser.Instance.Parse(string.Format(FallbackContent, BeatSaberMarkupLanguage.Utilities.EscapeXml(exception.Message)), contents, this);
			} catch (Exception fallbackException) {
				_siraLog.Error(fallbackException);
			}
		}

		private void BuildGrid() {
			_noteDirArrow.gameObject.name = "NoteDirArrow";
			_noteCutArrow.gameObject.name = "NoteCutArrow";
			_noteCutDistance.gameObject.name = "NoteCutDistance";

			_sdVersionText.text = $"SliceDetails v{ System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3) }";
			ReflectionUtil.InvokeMethod<object, TextMeshProUGUI>(_sdVersionText, "Awake"); // For some reason this is necessary
			_sdVersionText.rectTransform.sizeDelta = new Vector2(40.0f, 10.0f);
			_sdVersionText.transform.localPosition = new Vector3(0.0f, -17.0f, 0.0f);

			if (SceneManager.GetActiveScene().name == "MainMenu") {
				Destroy(_resetButtonTransform.gameObject);
			} else { 
				_resetButtonTransform.sizeDelta = new Vector2(8.0f, 4.0f);
				_resetButtonTransform.localPosition = new Vector3(-15.0f, -17.0f, 0.0f);
				_resetButtonTransform.GetComponentInChildren<CurvedTextMeshPro>().fontStyle = FontStyles.Normal;
				foreach (ImageView iv in _resetButtonTransform.GetComponentsInChildren<ImageView>()) {
					iv.SetField("_skew", 0.0f);
					iv.transform.localPosition = Vector3.zero;
				}
			}

			_tiles = new List<ClickableImage>();
			// Create first row of tiles
			for (int i = 0; i < 4; i++) {
				ClickableImage tileInstance = Instantiate(_tile.gameObject, _tileRow.transform).GetComponent<ClickableImage>();
				_tiles.Add(tileInstance);
			}

			// Create other 2 rows of tiles
			for (int i = 0; i < 2; i++) {
				GameObject tileRowInstance = Instantiate(_tileRow, _tileGrid.transform);
				tileRowInstance.transform.SetAsFirstSibling();
				_tiles.AddRange(tileRowInstance.GetComponentsInChildren<ClickableImage>());
			}

			// Set tile click events and data
			for (int i = 0; i < _tiles.Count; i++) {
				_tiles[i].OnClickEvent += SetNotesData;
				_tiles[i].DefaultColor = _tile.DefaultColor;
				_tiles[i].HighlightColor = _tile.HighlightColor;
			}

			Transform noteParent = _noteRow.transform;
			Transform rowParent = _noteGrid.transform;
			_notes = new List<NoteUI>();
			HoverHintController currentHoverHintController = _hoverHintControllerHandler.hoverHintController;
			for (int i = 0; i < 18; i++) {
				if (i % 9 == 0) {
					rowParent = Instantiate(_noteGrid, _noteHorizontal.transform).transform;
				}
				if (i % 3 == 0) {
					noteParent = Instantiate(_noteRow, rowParent).transform;
				}

				ColorType colorType = (ColorType)(i >= 9 ? 1 : 0);
				OrderedNoteCutDirection cutDirection = (OrderedNoteCutDirection)(i % 9);
				NoteUI uiNote = Instantiate(_note.gameObject, noteParent).AddComponent<NoteUI>();
				uiNote.Initialize(cutDirection, colorType, _assetLoader);
				uiNote.SetHoverHintController(currentHoverHintController);

				_notes.Add(uiNote);
			}

			_selectedTileIndicator = new GameObject("SelectedTileIndicator").AddComponent<SelectedTileIndicator>();
			_selectedTileIndicator.Initialize(_assetLoader);
			_selectedTileIndicator.transform.SetParent(_noteModal.transform, false);
			_selectedTileIndicator.transform.localPosition = new Vector3(0f, 30f, 0f);

			foreach (BasicUIAudioManager audioManager in Resources.FindObjectsOfTypeAll<BasicUIAudioManager>())
			{
				if (audioManager == null || audioManager.gameObject == null)
				{
					continue;
				}

				var audioSource = audioManager.GetComponent<AudioSource>();
				if (audioSource != null && audioSource.enabled && audioManager.gameObject.activeInHierarchy)
				{
					_basicUIAudioManager = audioManager;
					break;
				}
			}

			DestroyImmediate(_note.gameObject);
			DestroyImmediate(_noteRow);
			DestroyImmediate(_noteGrid);
			DestroyImmediate(_tile.gameObject);
		}

		public void SetTileScores() {
			Tile[] tiles = _sliceProcessor?.tiles;
			if (!_gridReady || tiles == null || _tiles == null)
			{
				return;
			}
			_refreshRevision = _sliceProcessor.revision;
			_refreshPending = !_sliceProcessor.ready && _sliceProcessor.processing;

			int tileCount = Math.Min(_tiles.Count, tiles.Length);
			for (int i = 0; i < tileCount; i++) {
				if (_tiles[i] == null || _tiles[i].gameObject == null || tiles[i] == null)
				{
					continue;
				}

				FormattableText[] texts = _tiles[i].transform.GetComponentsInChildren<FormattableText>(true);
				if (texts.Length == 0)
				{
					continue;
				}

				FormattableText scoreText = texts[0];
				FormattableText countText = texts.Length > 1 ? texts[1] : null;

				if(Plugin.Settings.ShowSliceCounts && countText != null) {
					scoreText.transform.localPosition = new Vector3(0.0f, 0.75f, 0.0f);
					countText.transform.localPosition = new Vector3(0.0f, -1.5f, 0.0f);
					countText.gameObject.SetActive(true);
				} else {
					if (countText != null)
					{
						countText.gameObject.SetActive(false);
					}
				}

				if (_sliceProcessor.ready && tiles[i].atLeastOneNote) {
					scoreText.text = String.Format("{0:0.00}", tiles[i].scoreAverage);
					if (countText != null)
					{
						countText.text = tiles[i].noteCount.ToString();
					}
				} else { 
					scoreText.text = "";
					if (countText != null)
					{
						countText.text = "";
					}
				}
			}
		}

		private void Update() {
			if (!_gridReady || _sliceProcessor == null || _tiles.Count == 0)
				return;
			if (_refreshRevision != _sliceProcessor.revision) {
				SetTileScores();
				return;
			}
			if (!_refreshPending)
				return;
			if (_sliceProcessor.ready)
				SetTileScores();
			else if (!_sliceProcessor.processing)
				_refreshPending = false;
		}

		private void SetNotesData(PointerEventData eventData) {
			if (!_gridReady || !_sliceProcessor.ready) {
				CloseModal(false);
				return;
			}
			int tileIndex = _tiles.IndexOf(eventData.pointerPress.GetComponent<ClickableImage>());
			if (tileIndex < 0 || tileIndex >= _sliceProcessor.tiles.Length || _selectedTileIndicator == null)
				return;
			_selectedTileIndicator.SetSelectedTile(tileIndex);
			Tile tile = _sliceProcessor.tiles[tileIndex];
			for (int i = 0; i < _notes.Count; i++) {
				float angle = tile.angleAverages[i];
				float offset = tile.offsetAverages[i];
				Score score = tile.scoreAverages[i];
				int count = tile.noteCounts[i];

				_notes[i].SetNoteData(angle, offset, score, count);
			}
		}

		[UIAction("#presentNotesModal")]
		public void PresentModal() {
			if (!_gridReady || !_sliceProcessor.ready) {
				CloseModal(false);
				return;
			}
			if (_basicUIAudioManager != null)
				_basicUIAudioManager.GetType().GetMethod("HandleButtonClickEvent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)?.Invoke(_basicUIAudioManager, null);
		}

		public void CloseModal(bool animated) {
			if (_noteModal == null)
			{
				return;
			}

			_noteModal.GetComponent<ModalView>()?.Hide(animated);
		}

		public void UpdateUINotesHoverHintController() {
			HoverHintController currentHoverHintController = _hoverHintControllerHandler.hoverHintController;
			for (int i = 0; i < _notes.Count; i++) {
				_notes[i].SetHoverHintController(currentHoverHintController);
			}
		}

		[UIAction("resetRecorder")]
		public void ResetRecorder() {
			SliceRecorder sliceRecorder = _diContainer.TryResolve<SliceRecorder>();
			sliceRecorder?.ClearSlices();
			SetTileScores();
		}
	}
}
