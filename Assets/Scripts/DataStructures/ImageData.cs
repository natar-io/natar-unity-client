public class ImageData {
    public int Width {get; set;} = -1;
    public int Height {get; set;} = -1;
    public int Channels {get; set;} = -1;
    public string Pixelformat {get; set;} = "";

    public ImageData(int width, int height, int channels, string pixelformat = "") {
        Width = width;
        Height = height;
        Channels = channels;
        Pixelformat = pixelformat;
    }

    public ImageData() {}
}
