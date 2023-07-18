﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using V2ex.Api;
using V2ex.Maui.Services;
using Volo.Abp.DependencyInjection;

namespace V2ex.Maui.Pages.ViewModels;

public partial class MyNodesPageViewModel : ObservableObject, IQueryAttributable, ITransientDependency
{
    [ObservableProperty]
    private string? _currentState;

    [ObservableProperty]
    private Exception? _exception;

    [ObservableProperty]
    private bool _canCurrentStateChange = true;

    [ObservableProperty]
    private List<MyNodesItemViewModel>? _nodes;

    public MyNodesPageViewModel(ApiService apiService)
    {
        this.ApiService = apiService;
    }

    private ApiService ApiService { get; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
    }

    [RelayCommand(CanExecute = nameof(CanCurrentStateChange))]
    public async Task Load(CancellationToken cancellationToken = default)
    {
        try
        {
            this.CurrentState = StateKeys.Loading;
            var nodeInfo = await this.ApiService.GetFavoriteNodes() ?? throw new InvalidOperationException("获取节点失败");
            this.Nodes = nodeInfo.Items
                .Select(o => InstanceActivator.Create<MyNodesItemViewModel>(o))
                .ToList();
            this.CurrentState = StateKeys.Success;
        }
        catch (Exception exception)
        {
            this.Exception = exception;
            this.CurrentState = StateKeys.Error;
        }
    }
}

public partial class MyNodesItemViewModel : ObservableObject
{
    public MyNodesItemViewModel(FavoriteNodeInfo.ItemInfo node,
        NavigationManager navigationManager)
    {
        this.Image = node.Image;
        this.Name = node.Name;
        this.Topics = node.Topics;
        this.Link = node.Link;
        this.Id = node.Id;
        this.NavigationManager = navigationManager;
    }

    [ObservableProperty]
    private string _id, _image, _name, _topics, _link;

    private NavigationManager NavigationManager { get; }

    [RelayCommand]
    public async Task GotoNode(CancellationToken cancellationToken)
    {
        await this.NavigationManager.GoToAsync(nameof(NodePage), true, new Dictionary<string, object> { { NodePageViewModel.QueryNodeKey, Id } });
    }
}