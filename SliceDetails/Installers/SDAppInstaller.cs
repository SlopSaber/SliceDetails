using BeatSaberMarkupLanguage.Settings;
using System;
using SliceDetails.UI;
using Zenject;

namespace SliceDetails.Installers
{
	public class SDAppInstaller : Installer<SDAppInstaller>
	{
		public override void InstallBindings() {
			Container.Bind<AssetLoader>().AsSingle().Lazy();
			Container.Bind<HoverHintControllerHandler>().AsSingle();
			Container.Bind<SliceProcessor>().AsSingle();
			Container.BindInterfacesAndSelfTo<SettingsMenu>().AsSingle();
		}

		private sealed class SettingsMenu : IInitializable, IDisposable {
			private readonly BSMLSettings _bsmlSettings;

			public SettingsMenu(BSMLSettings bsmlSettings) {
				_bsmlSettings = bsmlSettings;
			}

			public void Initialize() {
				_bsmlSettings.AddSettingsMenu("SliceDetails", "SliceDetails.UI.Views.settingsView.bsml", SettingsViewController.instance);
			}

			public void Dispose() {
				_bsmlSettings.RemoveSettingsMenu(SettingsViewController.instance);
			}
		}
	}
}
