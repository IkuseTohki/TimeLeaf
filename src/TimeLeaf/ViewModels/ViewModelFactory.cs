using System;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeLeaf.Services;
using TimeLeaf.UseCases;

namespace TimeLeaf.ViewModels;

/// <summary>
/// IViewModelFactory の具象実装。
/// 内部で IServiceProvider を使用し、依存関係を解決しながら ViewModel を生成する。
/// </summary>
public class ViewModelFactory : IViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ViewModelFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public OverviewViewModel CreateOverviewViewModel(ObservableCollection<ProjectViewModel> projects)
    {
        return new OverviewViewModel(
            projects,
            _serviceProvider.GetRequiredService<IAddProjectUseCase>(),
            _serviceProvider.GetRequiredService<LeafKit.UI.Services.IDialogService>(),
            _serviceProvider, // TODO: OverviewViewModel も ServiceProvider への依存を排除すべき
            _serviceProvider.GetRequiredService<ILogger<OverviewViewModel>>());
    }

    public ProjectWorkspaceViewModel CreateProjectWorkspaceViewModel(ProjectViewModel projectViewModel)
    {
        return new ProjectWorkspaceViewModel(
            projectViewModel,
            _serviceProvider.GetRequiredService<ICurrentUserService>(),
            _serviceProvider.GetRequiredService<ILogger<ProjectWorkspaceViewModel>>());
    }

    public AddProjectViewModel CreateAddProjectViewModel()
    {
        return _serviceProvider.GetRequiredService<AddProjectViewModel>();
    }
}
