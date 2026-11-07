using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using ProClubesApp.Models;
using ProClubesApp.ViewModels;

namespace ProClubesApp.Views;

public sealed class MaisGovernancaMobilePage : ContentPage
{
    private readonly MaisGovernancaViewModel _vm;

    public MaisGovernancaMobilePage(MaisGovernancaViewModel vm)
    {
        _vm = vm;
        BindingContext = _vm;
        Title = "Votações e Conta";
        BackgroundColor = ProClubesTheme.SurfaceApp;

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

                var opcoesContainer = new VerticalStackLayout { Spacing = 6 };
                opcoesContainer.SetBinding(BindableLayout.ItemsSourceProperty, nameof(VotacaoDto.Opcoes));
                BindableLayout.SetItemTemplate(opcoesContainer, new DataTemplate(() =>
                {
                    var btnOpcao = new Button
                    {
                        BackgroundColor = ProClubesTheme.BrandPrimary,
                        TextColor = Colors.White,
                        CornerRadius = 8,
                        FontSize = 12
                    };
                    btnOpcao.SetBinding(Button.TextProperty, nameof(OpcaoVotacaoDto.Texto), stringFormat: "Votar em: {0}");
                    btnOpcao.Clicked += async (s, _) =>
                    {
                        if (s is Button b && b.BindingContext is OpcaoVotacaoDto op && b.Parent?.BindingContext is VotacaoDto vot)
                        {
                            await _vm.RegistrarVotoDiretoAsync(vot.Id, op.Id);
                        }
                    };
                    return btnOpcao;
                }));

                return ProClubesTheme.CreateCard(new VerticalStackLayout
                {
                    Spacing = 6,
                    Children = { tit, desc, st, opcoesContainer }
                });
            })
        };

        var votacoesSection = ProClubesTheme.CreateCard(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label { Text = "Assembleias e Votações Digitais", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.BrandPrimary },
                new Label { Text = "Exclusivo para Associado Contribuinte elegível no congelamento do colégio eleitoral.", FontSize = 12, TextColor = ProClubesTheme.TextSecondary },
                votacoesList
            }
        });
        votacoesSection.SetBinding(VisualElement.IsVisibleProperty, nameof(MaisGovernancaViewModel.PodeVotar));

        var btnSair = new Button
        {
            Text = "Encerrar Sessão e Sair",
            BackgroundColor = ProClubesTheme.StatusDangerBg,
            TextColor = ProClubesTheme.StatusDanger,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold,
            Command = _vm.SairCommand
        };

        Content = new ScrollView
        {
            Padding = new Thickness(16),
            Content = new VerticalStackLayout
            {
                Children =
                {
                    ProClubesTheme.CreateOfflineBanner(),
                    ProClubesTheme.CreateStatusBanner(),
                    votacoesSection,
                    btnSair
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
