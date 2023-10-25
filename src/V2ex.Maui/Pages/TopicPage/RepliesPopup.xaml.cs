using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using Volo.Abp.DependencyInjection;

namespace V2ex.Maui.Pages;

public partial class RepliesPopup : Popup, ITransientDependency
{
    public RepliesPopup(IReadOnlyList<ReplyViewModel> replies)
    {
        this.BindingContext = new RepliesPopupViewModel(replies);
        InitializeComponent();
    }
}

public class RepliesPopupViewModel: ObservableObject
{
    public RepliesPopupViewModel(IReadOnlyList<ReplyViewModel> replies)
    {
        this.Replies = replies;
        this.Count = replies.Count;

        this.UserName = replies.FirstOrDefault()?.UserName;
    }
    public IReadOnlyList<ReplyViewModel> Replies { get;  }

    public int Count { get; }

    public string? UserName { get; }
}