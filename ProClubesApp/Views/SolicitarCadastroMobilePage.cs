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

        var headerGrid = ProClubesTheme.CreatePageHeader(
            "ADMISSÃO DE ASSOCIADOS",
            "Solicitar Filiação",
            "Envie seus dados para avaliação da diretoria da unidade",
            MobileVectorIconKind.CheckVote);

        var unidadePicker = new Picker
        {
            Title = "Selecione a unidade",
            TextColor = ProClubesTheme.TextPrimary,
            TitleColor = ProClubesTheme.TextMuted,
            ItemsSource = _vm.UnidadesPublicas,
            ItemDisplayBinding = new Binding(nameof(UnidadePublicaDto.Nome))
        };
        unidadePicker.SetBinding(Picker.SelectedItemProperty, nameof(LoginCadastroViewModel.UnidadeSelecionada));

        var categoriaPicker = new Picker
        {
            Title = "Selecione o tipo de associado",
            TextColor = ProClubesTheme.TextPrimary,
            TitleColor = ProClubesTheme.TextMuted,
            ItemsSource = _vm.OpcoesCategoria.ToList()
        };
        categoriaPicker.SetBinding(Picker.SelectedItemProperty, nameof(LoginCadastroViewModel.CategoriaSelecionada));

        var nomeEntry = new Entry { TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted, HeightRequest = 44 };
        nomeEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.NomeCompleto));

        var cpfEntry = new Entry { Keyboard = Keyboard.Numeric, MaxLength = 14, TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted, HeightRequest = 44 };
        cpfEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.Cpf));

        var sexoPicker = new Picker
        {
            Title = "-- Selecione --",
            TextColor = ProClubesTheme.TextPrimary,
            TitleColor = ProClubesTheme.TextMuted,
            ItemsSource = _vm.OpcoesSexo.ToList()
        };
        sexoPicker.SetBinding(Picker.SelectedItemProperty, nameof(LoginCadastroViewModel.Sexo));

        var estadoCivilPicker = new Picker
        {
            Title = "-- Selecione --",
            TextColor = ProClubesTheme.TextPrimary,
            TitleColor = ProClubesTheme.TextMuted,
            ItemsSource = _vm.OpcoesEstadoCivil.ToList()
        };
        estadoCivilPicker.SetBinding(Picker.SelectedItemProperty, nameof(LoginCadastroViewModel.EstadoCivil));

        var dataNascPicker = new DatePicker
        {
            Format = "dd/MM/yyyy",
            MaximumDate = DateTime.Today,
            MinimumDate = new DateTime(1920, 1, 1),
            TextColor = ProClubesTheme.TextPrimary,
            HeightRequest = 44
        };
        dataNascPicker.SetBinding(DatePicker.DateProperty, nameof(LoginCadastroViewModel.DataNascimento));

        var telEntry = new Entry { Keyboard = Keyboard.Telephone, MaxLength = 15, TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted, HeightRequest = 44 };
        telEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.Telefone));

        var emailCadEntry = new Entry { Keyboard = Keyboard.Email, TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted, HeightRequest = 44 };
        emailCadEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.Email));

        var senhaCadEntry = new Entry { IsPassword = true, TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted, HeightRequest = 44 };
        senhaCadEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.SenhaCadastro));

        // Campos Opcionais de Endereço
        var logradouroEntry = new Entry { TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted, HeightRequest = 44 };
        logradouroEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.Logradouro));

        var numeroEntry = new Entry { TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted, HeightRequest = 44 };
        numeroEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.Numero));

        var bairroEntry = new Entry { TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted, HeightRequest = 44 };
        bairroEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.Bairro));

        var cidadeEntry = new Entry { TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted, HeightRequest = 44 };
        cidadeEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.Cidade));

        var ufEntry = new Entry { MaxLength = 2, TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted, HeightRequest = 44 };
        ufEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.UF));

        var btnEnviarCadastro = new Button
        {
            Text = "Enviar Solicitação de Cadastro",
            BackgroundColor = ProClubesTheme.BrandSecondary,
            TextColor = Colors.White,
            CornerRadius = 10,
            HeightRequest = 44,
            FontAttributes = FontAttributes.Bold,
            Command = _vm.SolicitarCadastroCommand
        };

        var btnVoltarLogin = new Button
        {
            Text = "Já possui conta? Ir para Login",
            BackgroundColor = ProClubesTheme.SurfaceSubtle,
            TextColor = ProClubesTheme.BrandPrimary,
            CornerRadius = 10,
            HeightRequest = 42,
            FontAttributes = FontAttributes.Bold,
            Command = _vm.IrParaLoginCommand
        };

        var cadastroCard = ProClubesTheme.CreateCard(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label
                {
                    Text = "DADOS OBRIGATÓRIOS (*)",
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ProClubesTheme.TextMuted,
                    CharacterSpacing = 1
                },
                new Label
                {
                    Text = "Após o envio, você poderá acompanhar o status da sua solicitação fazendo login com seu CPF/e-mail e senha.",
                    FontSize = 12,
                    TextColor = ProClubesTheme.TextSecondary
                },
                CriarCampo("Unidade do Clube *", unidadePicker),
                CriarCampo("Tipo de Associado *", categoriaPicker),
                CriarCampo("Nome Completo *", nomeEntry),
                CriarCampo("CPF (11 dígitos) *", cpfEntry),
                CriarCampo("Sexo *", sexoPicker),
                CriarCampo("Estado Civil *", estadoCivilPicker),
                CriarCampo("Data de Nascimento *", dataNascPicker),
                CriarCampo("Telefone *", telEntry),
                CriarCampo("E-mail *", emailCadEntry),
                CriarCampo("Senha Inicial (mín. 6 caracteres) *", senhaCadEntry),
                new BoxView { HeightRequest = 1, Color = ProClubesTheme.BorderDefault, Margin = new Thickness(0, 6) },
                new Label
                {
                    Text = "ENDEREÇO (OPCIONAL)",
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ProClubesTheme.TextMuted,
                    CharacterSpacing = 1
                },
                CriarCampo("Logradouro", logradouroEntry),
                CriarCampo("Número", numeroEntry),
                CriarCampo("Bairro", bairroEntry),
                CriarCampo("Cidade", cidadeEntry),
                CriarCampo("UF (2 letras)", ufEntry),
                btnEnviarCadastro,
                btnVoltarLogin
            }
        });

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 18,
                Padding = new Thickness(20, 18, 20, 36),
                Children =
                {
                    headerGrid,
                    ProClubesTheme.CreateStatusBanner(),
                    cadastroCard
                }
            }
        };
    }

    private static VerticalStackLayout CriarCampo(string rotulo, View controle)
    {
        return new VerticalStackLayout
        {
            Spacing = 3,
            Children =
            {
                new Label
                {
                    Text = rotulo,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ProClubesTheme.TextPrimary
                },
                controle
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InicializarAsync();
    }
}
