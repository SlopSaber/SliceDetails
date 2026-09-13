using BeatSaberMarkupLanguage.Settings;
using System;
using SliceDetails.AffinityPatches;
using SliceDetails.UI;
using Zenject;

namespace SliceDetails.Installers
{
	internal class SDMenuInstaller : Installer<SDMenuInstaller>
	{
		public override void InstallBindings() {
			Container.BindInterfacesAndSelfTo<SettingsMenu>().AsSingle();
			Container.BindInterfacesAndSelfTo<HoverHintControllerGrabber>().AsSingle();
			Container.Bind<GridViewController>().FromNewComponentAsViewController().AsSingle();
			Container.Bind<UICreator>().AsSingle();

			Container.BindInterfacesTo<ResultsViewControllerPatch>().AsSingle();
			Container.BindInterfacesTo<PartyFreePlayFlowCoordinatorPatch>().AsSingle();
			Container.BindInterfacesTo<SoloFreePlayFlowCoordinatorPatch>().AsSingle();
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
