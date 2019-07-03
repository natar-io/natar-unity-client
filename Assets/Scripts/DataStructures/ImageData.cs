public class ImageData {
    public int Width {get; set;}
    public int Height {get; set;}
    public int Channels {get; set;}
    public string Pixelformat {get; set;}

    public ImageData(int width, int height, int channels, string pixelformat = "") {
        Width = width;
        Height = height;
        Channels = channels;
        Pixelformat = pixelformat;
    }

    public ImageData() {
        Width = -1;
        Height = -1;
        Channels = -1;
        Pixelformat = "";
    }
}
