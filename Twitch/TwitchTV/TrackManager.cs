using System;
using System.Collections.Generic;
using SM.Media.Playlists;

namespace SM.Media.BackgroundAudioStreamingAgent
{
    static class TrackManager
    {
        static MediaTrack[] GetPlaylist()
        {
            return new MediaTrack[]
            {
                new MediaTrack
                {
                    Title = "Starladder CS",
                    Url = new Uri("http://video10.iad02.hls.ttvnw.net/hls132/starladder_cs_en_13758889632_223696550/mobile/py-index-live.m3u8?token=id=7719667929993536106,bid=13758889632,exp=1427649363,node=video10-1.iad02.hls.justin.tv,nname=video10.iad02,fmt=mobile&sig=e31ede099a8dce344e0a2b8b0933e7568fe4f4ea")
                },
                new MediaTrack
                {
                    Title = "NPR",
                    Url = new Uri("http://www.npr.org/streams/mp3/nprlive24.pls")
                },
                new MediaTrack
                {
                    Title = "Bjarne Stroustrup - The Essence of C++",
                    Url = new Uri("http://media.ch9.ms/ch9/ca9a/66ac2da7-efca-4e13-a494-62843281ca9a/GN13BjarneStroustrup.mp3"),
                    UseNativePlayer = true
                },
                null,
                new MediaTrack
                {
                    Title = "Apple",
                    Url = new Uri("http://devimages.apple.com/iphone/samples/bipbop/bipbopall.m3u8")
                }
            };
        }

        public static IList<MediaTrack> Tracks
        {
            get { return GetPlaylist(); }
        }
    }
}
