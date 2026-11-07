using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using ProClubesApp.Models;
using ProClubesApp.ViewModels;

namespace ProClubesApp.Views;

public sealed class MaisGovernancaMobilePage : ContentPage
{
    private readonly MaisGovernancaViewModel _vm;
    private readonly PopupOverlayHost _popupHost;

    public MaisGovernancaMobilePage(MaisGovernancaViewModel vm)
    {
        _vm = vm;
        BindingContext = _vm;
        Title = "Votações";
        BackgroundColor = ProClubesTheme.SurfaceApp;

        var btnAtualizar = new Button
        {
            Text = "Atualizar Assembleias",
            BackgroundColor = ProClubesTheme.SurfaceSubtle,
            TextColor = ProClubesTheme.BrandPrimary,
            CornerRadius = 10,
            FontSize = 12,
            Command = _vm.AtualizarCommand
        };

        var abrirCedulaCmd = new Command<VotacaoDto>(votacao => AbrirPopupCedulaVotacao(votacao));

        var votacoesList = new CollectionView
        {
            ItemsSource = _vm.Votacoes,
            ItemTemplate = new DataTemplate(() =>
            {
                var tit = new Label { FontAttributes = FontAttributes.Bold, FontSize = 15, TextColor = ProClubesTheme.TextPrimary };
                tit.SetBinding(Label.TextProperty, nameof(VotacaoDto.Titulo));

                var desc = new Label { FontSize = 12, TextColor = ProClubesTheme.TextSecondary };
                desc.SetBinding(Label.TextProperty, nameof(VotacaoDto.Descricao));

                var st = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.BrandSecondary };
                st.SetBinding(Label.TextProperty, nameof(VotacaoDto.Estado), stringFormat: "Estado: {0}");

                var btnAbrirCedula = new Button
                {
                    Text = "Abrir Cédula de Votação",
                    BackgroundColor = ProClubesTheme.BrandPrimary,
                    TextColor = Colors.White,
                    CornerRadius = 8,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    Command = abrirCedulaCmd
                };
                btnAbrirCedula.SetBinding(Button.CommandParameterProperty, ".");

                var card = ProClubesTheme.CreateCard(new VerticalStackLayout
                {
                    Spacing = 6,
                    Children = { tit, desc, st, btnAbrirCedula }
                });

                var tap = new TapGestureRecognizer { Command = abrirCedulaCmd };
                tap.SetBinding(TapGestureRecognizer.CommandParameterProperty, ".");
                card.GestureRecognizers.Add(tap);

                return card;
            })
        };

        var mainScroll = new ScrollView
        {
            Padding = new Thickness(16),
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    ProClubesTheme.CreateOfflineBanner(),
                    ProClubesTheme.CreateStatusBanner(),
                    new Label { Text = "Assembleias e Votações Digitais", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary },
                    new Label { Text = "Toque em uma assembleia abaixo para abrir a cédula de votação secreta.", FontSize = 12, TextColor = ProClubesTheme.TextSecondary },
                    btnAtualizar,
                    votacoesList
                }
            }
        };

        _popupHost = new PopupOverlayHost(mainScroll);
        Content = _popupHost.RootLayout;
    }

    private void AbrirPopupCedulaVotacao(VotacaoDto? votacao)
    {
        if (votacao is null) return;
        _vm.ErrorMessage = null;
        _vm.SuccessMessage = null;

        OpcaoVotacaoDto? opcaoSelecionada = votacao.Opcoes.FirstOrDefault();

        var opcaoPicker = new Picker
        {
            Title = "Selecione sua opção de voto",
            TextColor = ProClubesTheme.TextPrimary,
            TitleColor = ProClubesTheme.TextMuted,
            ItemsSource = votacao.Opcoes.ToList(),
            ItemDisplayBinding = new Binding(nameof(OpcaoVotacaoDto.Texto)),
            SelectedItem = opcaoSelecionada
        };
        opcaoPicker.SelectedIndexChanged += (_, _) =>
        {
            opcaoSelecionada = opcaoPicker.SelectedItem as OpcaoVotacaoDto;
        };

        var btnConfirmarVoto = new Button
        {
            Text = "Confirmar Voto Secreto e Definitivo",
            BackgroundColor = ProClubesTheme.BrandSecondary,
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold
        };
        btnConfirmarVoto.Clicked += async (_, _) =>
        {
            if (opcaoSelecionada is null)
            {
                _vm.ErrorMessage = "Selecione uma opção antes de confirmar o voto.";
                return;
            }

            var ok = await _vm.RegistrarVotoDiretoAsync(votacao.Id, opcaoSelecionada.Id);
            if (ok)
            {
                _popupHost.Close();
            }
        };

        var btnCancelar = new Button
        {
            Text = "Cancelar",
            BackgroundColor = ProClubesTheme.SurfaceSubtle,
            TextColor = ProClubesTheme.TextSecondary,
            CornerRadius = 10
        };
        btnCancelar.Clicked += (_, _) =>
        {
            _vm.ErrorMessage = null;
            _popupHost.Close();
        };

        var body = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                ProClubesTheme.CreateStatusBanner(),
                new Label { Text = votacao.Titulo, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.BrandPrimary },
                new Label { Text = votacao.Descricao, FontSize = 13, TextColor = ProClubesTheme.TextPrimary },
                new Border
                {
                    BackgroundColor = ProClubesTheme.StatusInfoBg,
                    Stroke = Color.FromArgb("#BFDBFE"),
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
                    Padding = new Thickness(12, 8),
                    Content = new Label
                    {
                        Text = "🔒 Sigilo Garantido: sua presença é registrada separadamente da cédula. Uma vez confirmado, o voto não poderá ser alterado.",
                        FontSize = 12,
                        TextColor = ProClubesTheme.StatusInfo
                    }
                },
                new Label { Text = "Escolha sua opção na cédula:", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary },
                opcaoPicker,
                btnConfirmarVoto,
                btnCancelar
            }
        };

        _popupHost.Show("Cédula de Votação", body, () => _vm.ErrorMessage = null);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CarregarAsync();
    }
}
