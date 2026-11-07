using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using ProClubesApp.Models;
using ProClubesApp.ViewModels;

namespace ProClubesApp.Views;

public sealed class OuvidoriaMobilePage : ContentPage
{
    private readonly OuvidoriaViewModel _vm;

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
            FontAttributes = FontAttributes.Bold,
            Command = _vm.AlternarFormularioNovaCommand
        };

        var btnAtualizar = new Button
        {
            Text = "Atualizar Lista e Respostas",
            BackgroundColor = ProClubesTheme.SurfaceSubtle,
            TextColor = ProClubesTheme.BrandPrimary,
            CornerRadius = 10,
            FontSize = 12,
            Command = _vm.AtualizarCommand
        };

        var catPicker = new Picker
        {
            Title = "Categoria da Manifestação",
            TextColor = ProClubesTheme.TextPrimary,
            TitleColor = ProClubesTheme.TextMuted,
            ItemsSource = _vm.CategoriasManifestacao,
            ItemDisplayBinding = new Binding(nameof(CategoriaManifestacaoDto.Nome))
        };
        catPicker.SetBinding(Picker.SelectedItemProperty, nameof(OuvidoriaViewModel.CategoriaSelecionada));

        var assuntoEntry = new Entry { Placeholder = "Assunto da manifestação", TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        assuntoEntry.SetBinding(Entry.TextProperty, nameof(OuvidoriaViewModel.AssuntoManifestacao));

        var descManifEditor = new Editor { Placeholder = "Descreva sua dúvida, sugestão ou reclamação...", HeightRequest = 90, TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        descManifEditor.SetBinding(Editor.TextProperty, nameof(OuvidoriaViewModel.DescricaoManifestacao));

        var btnEnviarManif = new Button
        {
            Text = "Enviar Manifestação",
            BackgroundColor = ProClubesTheme.BrandSecondary,
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold,
            Command = _vm.EnviarManifestacaoCommand
        };

        var formCard = ProClubesTheme.CreateCard(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label { Text = "Registrar Nova Manifestação", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary },
                catPicker,
                assuntoEntry,
                descManifEditor,
                btnEnviarManif
            }
        });
        formCard.SetBinding(VisualElement.IsVisibleProperty, nameof(OuvidoriaViewModel.ExibirFormularioNova));

        // Card de Detalhes + Respostas da Diretoria quando o usuário clica em uma manifestação
        var lblDetTitulo = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.BrandPrimary };
        lblDetTitulo.SetBinding(Label.TextProperty, "ManifestacaoSelecionada.Titulo");

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
                BackgroundColor = ProClubesTheme.SurfaceCard,
                Stroke = ProClubesTheme.BorderDefault,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
                Padding = new Thickness(12, 8),
                Content = new VerticalStackLayout { Spacing = 4, Children = { cab, msg } }
            };
        }));

        var btnFecharDet = new Button
        {
            Text = "Fechar Respostas",
            BackgroundColor = ProClubesTheme.SurfaceCard,
            TextColor = ProClubesTheme.TextSecondary,
            CornerRadius = 8,
            FontSize = 12,
            Command = _vm.FecharDetalhesCommand
        };

        var detalhesCard = ProClubesTheme.CreateCard(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label { Text = "Respostas da Diretoria (Portal Web)", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.StatusInfo },
                lblDetTitulo,
                lblDetStatus,
                lblDetDesc,
                new BoxView { HeightRequest = 1, Color = ProClubesTheme.BorderDefault },
                lblSemResp,
                respostasContainer,
                btnFecharDet
            }
        }, ProClubesTheme.StatusInfoBg, Color.FromArgb("#BFDBFE"));
        detalhesCard.SetBinding(VisualElement.IsVisibleProperty, nameof(OuvidoriaViewModel.TemManifestacaoSelecionada));

        // Lista de cards clicáveis com as manifestações do associado
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
                    Command = _vm.SelecionarManifestacaoCommand
                };
                btnVerRespostas.SetBinding(Button.CommandParameterProperty, ".");

                var card = ProClubesTheme.CreateCard(new VerticalStackLayout
                {
                    Spacing = 6,
                    Children = { tit, meta, statusBadge, btnVerRespostas }
                });

                var tap = new TapGestureRecognizer { Command = _vm.SelecionarManifestacaoCommand };
                tap.SetBinding(TapGestureRecognizer.CommandParameterProperty, ".");
                card.GestureRecognizers.Add(tap);

                return card;
            })
        };

        Content = new ScrollView
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
                    new Label { Text = "Envie solicitações, sugestões ou reclamações e toque em qualquer card abaixo para consultar as respostas da diretoria.", FontSize = 12, TextColor = ProClubesTheme.TextSecondary },
                    btnNova,
                    btnAtualizar,
                    formCard,
                    detalhesCard,
                    new Label { Text = "Minhas Manifestações", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary, Margin = new Thickness(0, 6, 0, 2) },
                    list
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CarregarAsync();
    }
}
