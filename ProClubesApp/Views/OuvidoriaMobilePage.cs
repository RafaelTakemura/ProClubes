using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using ProClubesApp.Models;
using ProClubesApp.ViewModels;

namespace ProClubesApp.Views;

public sealed class OuvidoriaMobilePage : ContentPage
{
    private readonly OuvidoriaViewModel _vm;
    private readonly PopupOverlayHost _popupHost;

    public OuvidoriaMobilePage(OuvidoriaViewModel vm)
    {
        _vm = vm;
        BindingContext = _vm;
        Title = "Ouvidoria";
        BackgroundColor = ProClubesTheme.SurfaceApp;

        var btnNova = new Button
        {
            Text = "+ Nova Manifestação",
            BackgroundColor = ProClubesTheme.BrandPrimary,
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold
        };
        btnNova.Clicked += (_, _) => AbrirPopupNovaManifestacao();

        var btnAtualizar = new Button
        {
            Text = "Atualizar Lista e Respostas",
            BackgroundColor = ProClubesTheme.SurfaceSubtle,
            TextColor = ProClubesTheme.BrandPrimary,
            CornerRadius = 10,
            FontSize = 12,
            Command = _vm.AtualizarCommand
        };

        var abrirDetalhesCmd = new Command<ManifestacaoDto>(async item => await AbrirPopupDetalhesManifestacaoAsync(item));

        var list = new CollectionView
        {
            ItemsSource = _vm.MinhasManifestacoes,
            ItemTemplate = new DataTemplate(() =>
            {
                var tit = new Label { FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary };
                tit.SetBinding(Label.TextProperty, nameof(ManifestacaoDto.Titulo));

                var meta = new Label { FontSize = 12, TextColor = ProClubesTheme.TextSecondary };
                meta.SetBinding(Label.TextProperty, nameof(ManifestacaoDto.ResumoMeta));

                var statusBadge = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.BrandSecondary };
                statusBadge.SetBinding(Label.TextProperty, nameof(ManifestacaoDto.StatusBadge));

                var btnVerRespostas = new Button
                {
                    Text = "Ver Respostas da Diretoria",
                    BackgroundColor = ProClubesTheme.SurfaceSubtle,
                    TextColor = ProClubesTheme.BrandPrimary,
                    CornerRadius = 8,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    Command = abrirDetalhesCmd
                };
                btnVerRespostas.SetBinding(Button.CommandParameterProperty, ".");

                var card = ProClubesTheme.CreateCard(new VerticalStackLayout
                {
                    Spacing = 6,
                    Children = { tit, meta, statusBadge, btnVerRespostas }
                });

                var tap = new TapGestureRecognizer { Command = abrirDetalhesCmd };
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
                    new Label { Text = "Canal de Ouvidoria", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary },
                    new Label { Text = "Envie solicitações, sugestões ou reclamações e toque em qualquer card abaixo para abrir as respostas da diretoria em um pop-up.", FontSize = 12, TextColor = ProClubesTheme.TextSecondary },
                    btnNova,
                    btnAtualizar,
                    new Label { Text = "Minhas Manifestações", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary, Margin = new Thickness(0, 6, 0, 2) },
                    list
                }
            }
        };

        _popupHost = new PopupOverlayHost(mainScroll);
        Content = _popupHost.RootLayout;
    }

    private void AbrirPopupNovaManifestacao()
    {
        _vm.PrepararNovaManifestacao();

        var catPicker = new Picker
        {
            Title = "Categoria da Manifestação",
            TextColor = ProClubesTheme.TextPrimary,
            TitleColor = ProClubesTheme.TextMuted,
            ItemsSource = _vm.CategoriasManifestacao,
            ItemDisplayBinding = new Binding(nameof(CategoriaManifestacaoDto.Nome))
        };
        catPicker.SetBinding(Picker.SelectedItemProperty, nameof(OuvidoriaViewModel.CategoriaSelecionada));

        var assuntoEntry = new Entry
        {
            Placeholder = "Assunto da manifestação",
            TextColor = ProClubesTheme.TextPrimary,
            PlaceholderColor = ProClubesTheme.TextMuted
        };
        assuntoEntry.SetBinding(Entry.TextProperty, nameof(OuvidoriaViewModel.AssuntoManifestacao));

        var descManifEditor = new Editor
        {
            Placeholder = "Descreva sua dúvida, sugestão ou reclamação...",
            HeightRequest = 110,
            TextColor = ProClubesTheme.TextPrimary,
            PlaceholderColor = ProClubesTheme.TextMuted
        };
        descManifEditor.SetBinding(Editor.TextProperty, nameof(OuvidoriaViewModel.DescricaoManifestacao));

        var btnEnviar = new Button
        {
            Text = "Enviar Manifestação",
            BackgroundColor = ProClubesTheme.BrandSecondary,
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold
        };
        btnEnviar.Clicked += async (_, _) =>
        {
            var sucesso = await _vm.EnviarManifestacaoAsync();
            if (sucesso)
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
                new Label { Text = "Preencha os campos abaixo para encaminhar sua manifestação à diretoria da unidade.", FontSize = 12, TextColor = ProClubesTheme.TextSecondary },
                catPicker,
                assuntoEntry,
                descManifEditor,
                btnEnviar,
                btnCancelar
            }
        };

        _popupHost.Show("Nova Manifestação", body, () => _vm.ErrorMessage = null);
    }

    private async Task AbrirPopupDetalhesManifestacaoAsync(ManifestacaoDto? item)
    {
        if (item is null) return;
        await _vm.SelecionarManifestacaoAsync(item);

        var lblDetTitulo = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.BrandPrimary };
        lblDetTitulo.SetBinding(Label.TextProperty, "ManifestacaoSelecionada.Titulo");

        var lblDetMeta = new Label { FontSize = 12, TextColor = ProClubesTheme.TextSecondary };
        lblDetMeta.SetBinding(Label.TextProperty, "ManifestacaoSelecionada.ResumoMeta");

        var lblDetStatus = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.BrandSecondary };
        lblDetStatus.SetBinding(Label.TextProperty, "ManifestacaoSelecionada.StatusBadge");

        var lblDetDesc = new Label { FontSize = 13, TextColor = ProClubesTheme.TextPrimary };
        lblDetDesc.SetBinding(Label.TextProperty, "ManifestacaoSelecionada.Descricao");

        var lblSemResp = new Label
        {
            Text = "Ainda não há respostas registradas pela diretoria para esta manifestação.",
            FontSize = 12,
            FontAttributes = FontAttributes.Italic,
            TextColor = ProClubesTheme.TextSecondary
        };
        lblSemResp.SetBinding(VisualElement.IsVisibleProperty, nameof(OuvidoriaViewModel.SemRespostasNaSelecionada));

        var respostasContainer = new VerticalStackLayout { Spacing = 8 };
        respostasContainer.SetBinding(BindableLayout.ItemsSourceProperty, nameof(OuvidoriaViewModel.RespostasDaSelecionada));
        BindableLayout.SetItemTemplate(respostasContainer, new DataTemplate(() =>
        {
            var cab = new Label { FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.BrandPrimary };
            cab.SetBinding(Label.TextProperty, nameof(RespostaManifestacaoDto.CabecalhoFormatado));

            var msg = new Label { FontSize = 13, TextColor = ProClubesTheme.TextPrimary };
            msg.SetBinding(Label.TextProperty, nameof(RespostaManifestacaoDto.Mensagem));

            return new Border
            {
                BackgroundColor = ProClubesTheme.StatusInfoBg,
                Stroke = Color.FromArgb("#BFDBFE"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
                Padding = new Thickness(12, 8),
                Content = new VerticalStackLayout { Spacing = 4, Children = { cab, msg } }
            };
        }));

        var btnFechar = new Button
        {
            Text = "Fechar",
            BackgroundColor = ProClubesTheme.BrandPrimary,
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold
        };
        btnFechar.Clicked += (_, _) => _popupHost.Close();

        var body = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                lblDetTitulo,
                lblDetMeta,
                lblDetStatus,
                new Border
                {
                    BackgroundColor = ProClubesTheme.SurfaceSubtle,
                    Stroke = ProClubesTheme.BorderDefault,
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
                    Padding = new Thickness(12, 8),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children =
                        {
                            new Label { Text = "Sua mensagem:", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextSecondary },
                            lblDetDesc
                        }
                    }
                },
                new Label { Text = "Respostas da Diretoria:", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.StatusInfo, Margin = new Thickness(0, 4, 0, 0) },
                lblSemResp,
                respostasContainer,
                btnFechar
            }
        };

        _popupHost.Show("Respostas da Ouvidoria", body);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CarregarAsync();
    }
}
