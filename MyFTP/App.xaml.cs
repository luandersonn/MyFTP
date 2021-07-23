using Microsoft.Extensions.DependencyInjection;
using MyFTP.Services;
using MyFTP.ViewModels;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace MyFTP
{
	/// <resumo>
	///Fornece o comportamento específico do aplicativo para complementar a classe Application padrão.
	/// </summary>
	sealed partial class App : Application
	{
		/// <summary>
		/// Inicializa o objeto singleton do aplicativo. Essa é a primeira linha do código criado
		/// executado e, por isso, é o equivalente lógico de main() ou WinMain().
		/// </summary>
		public App()
		{
			Services = ConfigureServices();

			this.InitializeComponent();
			this.Suspending += OnSuspending;
			this.UnhandledException += OnUnhandledException;
		}
		/// <summary>
		/// Gets the current <see cref="App"/> instance in use
		/// </summary>
		public new static App Current => (App)Application.Current;

		/// <summary>
		/// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
		/// </summary>
		public IServiceProvider Services { get; }

		/// <summary>
		/// Configures the services for the application.
		/// </summary>
		private static IServiceProvider ConfigureServices()
		{
			var services = new ServiceCollection();

			services
				.AddSingleton<ISettings, AppSettings>()
				.AddTransient<LoginViewModel>();

			return services.BuildServiceProvider();
		}

		private async void OnUnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
		{
			//if(e.Exception is NotImplementedException)
			{
				e.Handled = true;
				await new ContentDialog
				{
					Title = e.Exception,
					Content = e.Message,
					CloseButtonText = "Close"
				}.ShowAsync();
			}
		}

		/// <summary>
		/// Invocado quando o aplicativo é iniciado normalmente pelo usuário final. Outros pontos de entrada
		/// serão usados, por exemplo, quando o aplicativo for iniciado para abrir um arquivo específico.
		/// </summary>
		/// <param name="e">Detalhes sobre a solicitação e o processo de inicialização.</param>
		protected override void OnLaunched(LaunchActivatedEventArgs e)
		{
			Frame rootFrame = Window.Current.Content as Frame;

			// Não repita a inicialização do aplicativo quando a Janela já tiver conteúdo,
			// apenas verifique se a janela está ativa
			if (rootFrame == null)
			{
				// Crie um Quadro para atuar como o contexto de navegação e navegue para a primeira página
				rootFrame = new Frame();

				rootFrame.NavigationFailed += OnNavigationFailed;
				if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
				{
					// TODO: Carregue o estado do aplicativo suspenso anteriormente
				}

				// Coloque o quadro na Janela atual
				Window.Current.Content = rootFrame;
			}

			if (e.PrelaunchActivated == false)
			{
				if (rootFrame.Content == null)
				{
					// Quando a pilha de navegação não for restaurada, navegar para a primeira página,
					// configurando a nova página passando as informações necessárias como um parâmetro
					// de navegação
					rootFrame.Navigate(typeof(Views.LoginViewPage), e.Arguments);


				}
				// Verifique se a janela atual está ativa
				Window.Current.Activate();
			}
			ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(500, 500));
		}

		/// <summary>
		/// Chamado quando ocorre uma falha na Navegação para uma determinada página
		/// </summary>
		/// <param name="sender">O Quadro com navegação com falha</param>
		/// <param name="e">Detalhes sobre a falha na navegação</param>
		void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
		{
			throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
		}

		/// <summary>
		/// Invocado quando a execução do aplicativo é suspensa. O estado do aplicativo é salvo
		/// sem saber se o aplicativo será encerrado ou retomado com o conteúdo
		/// da memória ainda intacto.
		/// </summary>
		/// <param name="sender">A origem da solicitação de suspensão.</param>
		/// <param name="e">Detalhes sobre a solicitação de suspensão.</param>
		private void OnSuspending(object sender, SuspendingEventArgs e)
		{
			var deferral = e.SuspendingOperation.GetDeferral();
			//TODO: Salvar o estado do aplicativo e parar qualquer atividade em segundo plano
			deferral.Complete();
		}
	}
}
