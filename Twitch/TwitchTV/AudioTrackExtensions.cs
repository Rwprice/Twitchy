using Microsoft.Phone.BackgroundAudio;

namespace SM.Media.BackgroundAudioStreamingAgent
{
    public static class AudioTrackExtensions
    {
        public static string ToExtendedString(this AudioTrack track)
        {
            if (null == track)
                return "<no track>";

            return string.Format("AudioTrack source {0} tag {1}",
                null == track.Source ? "<none>" : track.Source.ToString(), track.Tag);
        }
    }
}
