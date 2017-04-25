using Uterm.Tools;

namespace UTerm
{
    public interface ITerminal
    {
        bool InputEnabled { get; set; }
        bool HasInput { get; }

        void PlaceCaret(CaretCoord coord);
        void WriteLine(string line);
        byte ReadLastInput();
        void Tick();
    }

    public interface IOutputDevice
    {
        void SetText(string text);
    }
}