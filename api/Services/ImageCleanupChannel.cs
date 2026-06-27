using System.Threading.Channels;

namespace PCS_API.Services;

public class ImageCleanupChannel
{
    private readonly Channel<List<string>> _channel = Channel.CreateUnbounded<List<string>>();

    public ChannelWriter<List<string>> Writer => _channel.Writer;
    public ChannelReader<List<string>> Reader => _channel.Reader;
}
