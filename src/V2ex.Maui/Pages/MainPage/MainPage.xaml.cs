using Bogus;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using V2ex.Maui.Services;
using Volo.Abp.DependencyInjection;

namespace V2ex.Maui.Pages;

public partial class MainPage : ContentPage, ITransientDependency
{
    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        this.BindingContext = viewModel;
        ModifyEntry();
    }

    void ModifyEntry()
    {
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("MyCustomization", (handler, view) =>
        {
#if ANDROID
            handler.PlatformView.SetSelectAllOnFocus(true);
#elif IOS || MACCATALYST
            handler.PlatformView.EditingDidBegin += (s, e) =>
            {
                handler.PlatformView.PerformSelector(new ObjCRuntime.Selector("selectAll"), null, 0.0f);
            };
#elif WINDOWS
            handler.PlatformView.GotFocus += (s, e) =>
            {
                handler.PlatformView.SelectAll();
            };
#endif
        });
    }
}

public class MainPageViewModel: ObservableObject, ITransientDependency
{
    public ObservableCollection<ImageItemViewModel> Images { get;  } = new ();

    public MainPageViewModel()
    {
        Faker faker = new Faker();
        foreach (var index in Enumerable.Range(0, 100))
        {
            var imageSize = faker.Random.Number(100, 500);
            this.Images.Add(new ImageItemViewModel
            {
                Index = index,
                Title = faker.Lorem.Paragraphs(faker.Random.Number(5)),
                Image = faker.Image.PicsumUrl(imageSize, imageSize)
            });
        } 
    }

}

public partial class ImageItemViewModel: ObservableObject
{
    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    private string? _title, _image;
}