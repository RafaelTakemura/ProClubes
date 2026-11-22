using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using ProClubesApp.Models;
using ProClubesApp.ViewModels;

namespace ProClubesApp.Views;

/// <summary>
/// Tela de gerenciamento de dependentes do associado titular com envio para aprovação pela API.
/// </summary>
public sealed class DependentesMobilePage : ContentPage
{
    private readonly DependentesViewModel _vm;
    private readonly PopupOverlayHost _popupHost;

    public DependentesMobilePage(DependentesViewModel vm)
    {
        _vm = vm;
        BindingContext = _vm;
        Title = "Dependentes";
        BackgroundColor = ProClubesTheme.SurfaceApp;

        var headerGrid = ProClubesTheme.CreatePageHeader(
            "VÍNCULOS FAMILIARES",
            "Dependentes",
            "Cadastre dependentes e acompanhe a aprovação pela diretoria",
            MobileVectorIconKind.CheckVote);

        var btnSolicitar = new Button
        {
            Text = "+ Solicitar Dependente",
            BackgroundColor = ProClubesTheme.BrandSecondary,
            TextColor = Colors.White,
            CornerRadius = 10,
            HeightRequest = 42,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold
        };
        btnSolicitar.Clicked += async (_, _) => await AbrirPopupFormularioDependenteAsync(null);

        var btnAtualizar = new Button
        {
            Text = "Atualizar",
            BackgroundColor = ProClubesTheme.SurfaceCard,
            TextColor = ProClubesTheme.BrandPrimary,
            BorderColor = ProClubesTheme.BorderDefault,
            BorderWidth = 1,
            CornerRadius = 10,
            HeightRequest = 42,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            Command = _vm.AtualizarCommand
        };

        var actionsGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 10
        };
        actionsGrid.Add(btnSolicitar, 0, 0);
        actionsGrid.Add(btnAtualizar, 1, 0);

        var lblResumoContagem = new Label
        {
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = ProClubesTheme.TextPrimary
        };
        lblResumoContagem.SetBinding(Label.TextProperty, nameof(DependentesViewModel.ResumoContagem));

        var resumoCard = ProClubesTheme.CreateCard(new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label
                {
                    Text = "RESUMO DO GRUPO FAMILIAR",
                    FontSize = 10,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ProClubesTheme.TextMuted,
                    CharacterSpacing = 1
                },
                lblResumoContagem,
                new Label
                {
                    Text = "Novos dependentes passam pela validação da diretoria antes da liberação de acesso ao clube.",
                    FontSize = 12,
                    TextColor = ProClubesTheme.TextSecondary
                }
            }
        });

        var emptyCard = ProClubesTheme.CreateCard(new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label
                {
                    Text = "Nenhum dependente vinculado",
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ProClubesTheme.TextPrimary
                },
                new Label
                {
                    Text = "Toque em '+ Solicitar Dependente' para vincular cônjuge ou filho(a) até 21 anos.",
                    FontSize = 12,
                    TextColor = ProClubesTheme.TextMuted
                }
            }
        });
        emptyCard.SetBinding(VisualElement.IsVisibleProperty, nameof(DependentesViewModel.SemDependentes));

        var depStack = new VerticalStackLayout { Spacing = 0 };
        depStack.SetBinding(VisualElement.IsVisibleProperty, nameof(DependentesViewModel.TemDependentes));
        BindableLayout.SetItemsSource(depStack, _vm.Dependentes);
        BindableLayout.SetItemTemplate(depStack, new DataTemplate(() =>
        {
            var lblIniciais = new Label
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = ProClubesTheme.TextPrimary
            };
            lblIniciais.SetBinding(Label.TextProperty, nameof(DependenteMobileDto.Iniciais));

            var avatarBorder = new Border
            {
                BackgroundColor = ProClubesTheme.SurfaceSubtle,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(999) },
                WidthRequest = 38,
                HeightRequest = 38,
                VerticalOptions = LayoutOptions.Center,
                Content = lblIniciais
            };

            var lblNome = new Label
            {
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = ProClubesTheme.TextPrimary
            };
            lblNome.SetBinding(Label.TextProperty, nameof(DependenteMobileDto.NomeCompleto));

            var lblSubtitulo = new Label
            {
                FontSize = 12,
                TextColor = ProClubesTheme.TextMuted
            };
            lblSubtitulo.SetBinding(Label.TextProperty, nameof(DependenteMobileDto.SubtituloFormatado));

            var lblProfissao = new Label
            {
                FontSize = 12,
                TextColor = ProClubesTheme.TextSecondary
            };
            lblProfissao.SetBinding(Label.TextProperty, nameof(DependenteMobileDto.ProfissaoFormatada), stringFormat: "Profissão: {0}");

            var statusPill = CreateStatusPill(new Thickness(10, 3), 11);

            var topGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 12,
                VerticalOptions = LayoutOptions.Center
            };
            topGrid.Add(avatarBorder, 0, 0);
            topGrid.Add(new VerticalStackLayout
            {
                Spacing = 2,
                VerticalOptions = LayoutOptions.Center,
                Children = { lblNome, lblSubtitulo, lblProfissao }
            }, 1, 0);
            topGrid.Add(statusPill, 2, 0);

            var lblAuditoria = new Label
            {
                FontSize = 11,
                TextColor = ProClubesTheme.TextMuted
            };
            lblAuditoria.SetBinding(Label.TextProperty, nameof(DependenteMobileDto.AuditoriaResumo));

            var btnEditarDados = new Button
            {
                Text = "Editar Dados Cadastrais",
                BackgroundColor = ProClubesTheme.SurfaceSubtle,
                TextColor = ProClubesTheme.BrandPrimary,
                BorderColor = ProClubesTheme.BorderDefault,
                BorderWidth = 1,
                CornerRadius = 8,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                HeightRequest = 38
            };
            btnEditarDados.Clicked += async (_, _) =>
            {
                if (btnEditarDados.BindingContext is DependenteMobileDto depSelecionado)
                    await AbrirPopupFormularioDependenteAsync(depSelecionado);
            };

            var btnAlterarStatus = new Button
            {
                CornerRadius = 8,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                HeightRequest = 38,
                BorderColor = ProClubesTheme.BorderDefault,
                BorderWidth = 1,
                Command = _vm.AlterarStatusCommand
            };
            btnAlterarStatus.SetBinding(Button.CommandParameterProperty, ".");
            btnAlterarStatus.SetBinding(Button.TextProperty, nameof(DependenteMobileDto.AcaoStatusTexto));
            btnAlterarStatus.SetBinding(Button.TextColorProperty, nameof(DependenteMobileDto.AcaoStatusCorTexto));
            btnAlterarStatus.SetBinding(VisualElement.BackgroundColorProperty, nameof(DependenteMobileDto.AcaoStatusCorFundo));
            btnAlterarStatus.SetBinding(VisualElement.IsVisibleProperty, nameof(DependenteMobileDto.PodeAlterarStatus));
            btnAlterarStatus.BindingContextChanged += (_, _) =>
            {
                if (btnAlterarStatus.BindingContext is DependenteMobileDto d)
                {
                    btnAlterarStatus.TextColor = d.AcaoStatusCorTexto;
                    btnAlterarStatus.BackgroundColor = d.AcaoStatusCorFundo;
                }
            };

            var lblAguardando = new Label
            {
                Text = "Aguardando avaliação da diretoria",
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = ProClubesTheme.StatusWarning
            };
            lblAguardando.SetBinding(VisualElement.IsVisibleProperty, nameof(DependenteMobileDto.PendenteAprovacao));

            return ProClubesTheme.CreateCard(new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    topGrid,
                    lblAuditoria,
                    new BoxView { HeightRequest = 1, Color = ProClubesTheme.BorderDefault },
                    btnEditarDados,
                    btnAlterarStatus,
                    lblAguardando
                }
            });
        }));

        var mainScroll = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 14,
                Padding = new Thickness(20, 18, 20, 36),
                Children =
                {
                    ProClubesTheme.CreateOfflineBanner(),
                    ProClubesTheme.CreateStatusBanner(),
                    headerGrid,
                    actionsGrid,
                    resumoCard,
                    new Label
                    {
                        Text = "DEPENDENTES VINCULADOS",
                        FontSize = 11,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = ProClubesTheme.TextMuted,
                        CharacterSpacing = 1,
                        Margin = new Thickness(0, 2, 0, 0)
                    },
                    emptyCard,
                    depStack
                }
            }
        };

        _popupHost = new PopupOverlayHost(mainScroll);
        Content = _popupHost.RootLayout;
    }

    internal static Border CreateStatusPill(Thickness? padding = null, double fontSize = 11)
    {
        var lblStatus = new Label
        {
            FontSize = fontSize,
            FontAttributes = FontAttributes.Bold,
            TextColor = ProClubesTheme.StatusSuccess
        };
        lblStatus.SetBinding(Label.TextProperty, nameof(DependenteMobileDto.StatusTexto));
        lblStatus.SetBinding(Label.TextColorProperty, nameof(DependenteMobileDto.StatusCorTexto));

        var statusPill = ProClubesTheme.CreateSoftTag(
            lblStatus,
            ProClubesTheme.StatusSuccessBg,
            ProClubesTheme.StatusSuccessBorder,
            padding ?? new Thickness(10, 3));

        statusPill.SetBinding(VisualElement.BackgroundColorProperty, nameof(DependenteMobileDto.StatusCorFundo));
        statusPill.SetBinding(Border.StrokeProperty, nameof(DependenteMobileDto.StatusCorBorda));
        statusPill.BindingContextChanged += (_, _) =>
        {
            if (statusPill.BindingContext is DependenteMobileDto d)
            {
                lblStatus.TextColor = d.StatusCorTexto;
                statusPill.BackgroundColor = d.StatusCorFundo;
                statusPill.Stroke = d.StatusCorBorda;
            }
        };

        return statusPill;
    }

    private async Task AbrirPopupFormularioDependenteAsync(DependenteMobileDto? depEdicao)
    {
        var ehEdicao = depEdicao is not null;
        if (ehEdicao)
        {
            if (_vm.IsOffline)
            {
                await DisplayAlertAsync("Modo Offline", "A edição de dependentes exige conexão com a internet.", "OK");
                return;
            }
            await _vm.PrepararEdicaoDependenteAsync(depEdicao!);
        }
        else
        {
            await _vm.PrepararNovoDependenteAsync();
        }

        var vinculoPicker = new Picker
        {
            Title = "Selecione o vínculo",
            TextColor = ProClubesTheme.TextPrimary,
            TitleColor = ProClubesTheme.TextMuted,
            ItemsSource = _vm.OpcoesTipoVinculo.ToList()
        };
        vinculoPicker.SetBinding(Picker.SelectedItemProperty, nameof(DependentesViewModel.TipoVinculoSelecionado));

        var nomeEntry = new Entry { TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted, HeightRequest = 42 };
        nomeEntry.SetBinding(Entry.TextProperty, nameof(DependentesViewModel.NomeCompleto));

        var cpfEntry = ehEdicao
            ? new Entry
            {
                IsReadOnly = true,
                IsEnabled = false,
                BackgroundColor = ProClubesTheme.SurfaceSubtle,
                TextColor = ProClubesTheme.TextMuted,
                HeightRequest = 42
            }
            : new Entry { TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted, HeightRequest = 42 };

        if (ehEdicao)
        {
            cpfEntry.SetBinding(Entry.TextProperty, nameof(DependentesViewModel.CpfSomenteLeitura));
        }
        else
        {
            ProClubesTheme.ApplyCpfCnpjMask(cpfEntry);
            cpfEntry.SetBinding(Entry.TextProperty, nameof(DependentesViewModel.Cpf));
        }

        var sexoPicker = new Picker
        {
            Title = "-- Selecione --",
            TextColor = ProClubesTheme.TextPrimary,
            TitleColor = ProClubesTheme.TextMuted,
            ItemsSource = _vm.OpcoesSexo.ToList()
        };
        sexoPicker.SetBinding(Picker.SelectedItemProperty, nameof(DependentesViewModel.Sexo));

        var estadoCivilPicker = new Picker
        {
            Title = "-- Selecione --",
            TextColor = ProClubesTheme.TextPrimary,
            TitleColor = ProClubesTheme.TextMuted,
            ItemsSource = _vm.OpcoesEstadoCivil.ToList()
        };
        estadoCivilPicker.SetBinding(Picker.SelectedItemProperty, nameof(DependentesViewModel.EstadoCivil));

        var dataNascPicker = new DatePicker
        {
            Format = "dd/MM/yyyy",
            MaximumDate = DateTime.Today,
            MinimumDate = new DateTime(1920, 1, 1),
            TextColor = ProClubesTheme.TextPrimary,
            HeightRequest = 42
        };
        dataNascPicker.SetBinding(DatePicker.DateProperty, nameof(DependentesViewModel.DataNascimento));

        var profissaoEntry = new Entry { Placeholder = "Ex.: Estudante, Arquiteto(a)...", TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted, HeightRequest = 42 };
        profissaoEntry.SetBinding(Entry.TextProperty, nameof(DependentesViewModel.Profissao));

        var telEntry = new Entry { Keyboard = Keyboard.Telephone, MaxLength = 15, TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted, HeightRequest = 42 };
        telEntry.SetBinding(Entry.TextProperty, nameof(DependentesViewModel.Telefone));

        var emailEntry = new Entry { Keyboard = Keyboard.Email, TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted, HeightRequest = 42 };
        emailEntry.SetBinding(Entry.TextProperty, nameof(DependentesViewModel.Email));

        var logradouroEntry = new Entry { TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted, HeightRequest = 42 };
        logradouroEntry.SetBinding(Entry.TextProperty, nameof(DependentesViewModel.Logradouro));

        var numeroEntry = new Entry { TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted, HeightRequest = 42 };
        numeroEntry.SetBinding(Entry.TextProperty, nameof(DependentesViewModel.Numero));

        var bairroEntry = new Entry { TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted, HeightRequest = 42 };
        bairroEntry.SetBinding(Entry.TextProperty, nameof(DependentesViewModel.Bairro));

        var localidadesSection = ProClubesTheme.CreateLocalidadePickers(_vm.Localidades);

        var btnEnviar = new Button
        {
            Text = ehEdicao ? "Salvar Alterações" : "Enviar para Aprovação",
            BackgroundColor = ProClubesTheme.BrandSecondary,
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 42
        };
        btnEnviar.Clicked += async (_, _) =>
        {
            var ok = ehEdicao
                ? await _vm.SalvarEdicaoDependenteAsync()
                : await _vm.SolicitarDependenteAsync();
            if (ok)
                _popupHost.Close();
        };

        var btnCancelar = new Button
        {
            Text = "Cancelar",
            BackgroundColor = ProClubesTheme.SurfaceSubtle,
            TextColor = ProClubesTheme.TextSecondary,
            CornerRadius = 10,
            HeightRequest = 40
        };
        btnCancelar.Clicked += (_, _) =>
        {
            _vm.ErrorMessage = null;
            _popupHost.Close();
        };

        static VerticalStackLayout CriarCampoPopup(string rotulo, View controle) => ProClubesTheme.CreateFormField(rotulo, controle);

        var body = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                ProClubesTheme.CreateStatusBanner(),
                new Label
                {
                    Text = "DADOS OBRIGATÓRIOS (*)",
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ProClubesTheme.TextMuted,
                    CharacterSpacing = 1
                },
                CriarCampoPopup("Tipo de Vínculo *", vinculoPicker),
                CriarCampoPopup("Nome Completo *", nomeEntry),
                CriarCampoPopup(ehEdicao ? "CPF (não alterável)" : "CPF (11 dígitos) *", cpfEntry),
                CriarCampoPopup("Sexo *", sexoPicker),
                CriarCampoPopup("Estado Civil *", estadoCivilPicker),
                CriarCampoPopup("Data de Nascimento *", dataNascPicker),
                CriarCampoPopup("Profissão *", profissaoEntry),
                CriarCampoPopup("Telefone *", telEntry),
                CriarCampoPopup("E-mail *", emailEntry),
                new BoxView { HeightRequest = 1, Color = ProClubesTheme.BorderDefault, Margin = new Thickness(0, 4) },
                new Label
                {
                    Text = "ENDEREÇO (OPCIONAL)",
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ProClubesTheme.TextMuted,
                    CharacterSpacing = 1
                },
                CriarCampoPopup("Logradouro", logradouroEntry),
                CriarCampoPopup("Número", numeroEntry),
                CriarCampoPopup("Bairro", bairroEntry),
                localidadesSection,
                btnEnviar,
                btnCancelar
            }
        };

        _popupHost.Show(
            ehEdicao ? $"Editar Dependente — {depEdicao!.NomeCompleto}" : "Solicitar Cadastro de Dependente",
            body,
            () => _vm.ErrorMessage = null);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CarregarAsync();
    }
}
