namespace DemoApp.Models;

public sealed class DynamicTextTileViewModel : DynamicTileViewModel
{
    private string _text = "Generated from ItemsSource";

    public string Text
    {
        get { return _text; }
        set { SetField(ref _text, value); }
    }
}
