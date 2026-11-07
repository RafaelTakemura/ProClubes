using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using ProClubesApp.Models;
using ProClubesApp.ViewModels;

namespace ProClubesApp.Views;

public sealed class SolicitarCadastroMobilePage : ContentPage
{
    private readonly LoginCadastroViewModel _vm;

    public SolicitarCadastroMobilePage(LoginCadastroViewModel vm)
    {
        _vm = vm;
        BindingContext = _vm;
        Title = "Solicitar Cadastro";
        BackgroundColor = ProClubesTheme.SurfaceApp;

        var headerCard = ProClubesTheme.CreateCard(new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = "Solicitar Filiação ao Clube", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                new Label { Text = "Preencha seus dados para análise da secretaria da unidade escolhida.", FontSize = 12, TextColor = Color.FromArgb("#CBD5E1") }
            }
        }, ProClubesTheme.BrandPrimary, ProClubesTheme.BrandPrimaryHover);

        var unidadePicker = new Picker
        {
            Title = "Unidade do Clube pretendida",
            TextColor = ProClubesTheme.TextPrimary,
            TitleColor = ProClubesTheme.TextMuted,
            ItemsSource = _vm.UnidadesPublicas,
            ItemDisplayBinding = new Binding(nameof(UnidadePublicaDto.Nome))
        };
        unidadePicker.SetBinding(Picker.SelectedItemProperty, nameof(LoginCadastroViewModel.UnidadeSelecionada));

        var nomeEntry = new Entry { Placeholder = "Nome completo", TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        nomeEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.NomeCompleto));

        var cpfEntry = new Entry { Placeholder = "CPF (11 dígitos)", Keyboard = Keyboard.Numeric, TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        cpfEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.Cpf));

        var emailCadEntry = new Entry { Placeholder = "E-mail", Keyboard = Keyboard.Email, TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        emailCadEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.Email));

        var telEntry = new Entry { Placeholder = "Telefone celular", Keyboard = Keyboard.Telephone, TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        telEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.Telefone));

        var senhaCadEntry = new Entry { Placeholder = "Senha de acesso (mín. 8 caracteres)", IsPassword = true, TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        senhaCadEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.SenhaCadastro));

        var btnEnviarCadastro = new Button
        {
            Text = "Enviar Solicitação de Cadastro",
            BackgroundColor = ProClubesTheme.BrandSecondary,
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold,
            Command = _vm.SolicitarCadastroCommand
        };

        var btnVoltarLogin = new Button
        {
            Text = "Já possui conta? Ir para Login",
            BackgroundColor = ProClubesTheme.SurfaceSubtle,
            TextColor = ProClubesTheme.BrandPrimary,
            CornerRadius = 10,
            Command = _vm.IrParaLoginCommand
        };

        var cadastroCard = ProClubesTheme.CreateCard(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label { Text = "Solicitar Cadastro de Associado", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary },
                new Label { Text = "Após o envio, você poderá acompanhar o status da sua solicitação fazendo login com seu CPF/e-mail e senha.", FontSize = 12, TextColor = ProClubesTheme.TextSecondary },
                unidadePicker,
                nomeEntry,
                cpfEntry,
                emailCadEntry,
                telEntry,
                senhaCadEntry,
                btnEnviarCadastro,
                btnVoltarLogin
            }
        });

        Content = new ScrollView
        {
            Padding = new Thickness(16),
            Content = new VerticalStackLayout
            {
                Children =
                {
                    headerCard,
                    ProClubesTheme.CreateStatusBanner(),
                    cadastroCard
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InicializarAsync();
    }
}
