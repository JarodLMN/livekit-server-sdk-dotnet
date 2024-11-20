namespace Livekit.Server.Sdk.Dotnet.Test
{
    [Collection("Integration tests")]
    public class EgressServiceClientTest
    {
        private ServiceClientFixture fixture;

        public EgressServiceClientTest(ServiceClientFixture fixture)
        {
            this.fixture = fixture;
        }

        private readonly EgressServiceClient egressClient = new EgressServiceClient(
            ServiceClientFixture.TEST_HTTP_URL,
            ServiceClientFixture.TEST_API_KEY,
            ServiceClientFixture.TEST_API_SECRET
        );
        private readonly RoomServiceClient roomClient = new RoomServiceClient(
            ServiceClientFixture.TEST_HTTP_URL,
            ServiceClientFixture.TEST_API_KEY,
            ServiceClientFixture.TEST_API_SECRET
        );

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "EgressService")]
        public async Task List_Egress()
        {
            var response = await egressClient.ListEgress(new ListEgressRequest());
            Assert.NotNull(response.Items);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "EgressService")]
        public async Task Start_RoomComposite_Egress()
        {
            await roomClient.CreateRoom(new CreateRoomRequest { Name = TestConstants.ROOM_NAME });
            var request = new RoomCompositeEgressRequest { RoomName = TestConstants.ROOM_NAME };
            request.FileOutputs.Add(
                new EncodedFileOutput { FileType = EncodedFileType.Mp4, Filepath = "/tmp/test.mp4" }
            );
            var egress = await egressClient.StartRoomCompositeEgress(request);
            Assert.NotNull(egress);
            Assert.Equal(TestConstants.ROOM_NAME, egress.RoomName);
            Assert.Single(egress.FileResults);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "EgressService")]
        public async Task Start_TrackComposite_Egress()
        {
            await roomClient.CreateRoom(new CreateRoomRequest { Name = TestConstants.ROOM_NAME });
            await fixture.PublishVideoTrackInRoom(
                roomClient,
                TestConstants.ROOM_NAME,
                TestConstants.PARTICIPANT_IDENTITY
            );
            var participant = await roomClient.GetParticipant(
                new RoomParticipantIdentity
                {
                    Room = TestConstants.ROOM_NAME,
                    Identity = TestConstants.PARTICIPANT_IDENTITY,
                }
            );
            var videoTrack = participant
                .Tracks.Where(t => t.Type == TrackType.Video)
                .FirstOrDefault();
            Assert.NotNull(videoTrack);
            var request = new TrackCompositeEgressRequest
            {
                RoomName = TestConstants.ROOM_NAME,
                VideoTrackId = videoTrack.Sid,
                FileOutputs =
                {
                    new EncodedFileOutput
                    {
                        FileType = EncodedFileType.Mp4,
                        Filepath = "/tmp/test.mp4",
                    },
                },
            };
            var egress = await egressClient.StartTrackCompositeEgress(request);
            Assert.NotNull(egress);
            Assert.Equal(TestConstants.ROOM_NAME, egress.RoomName);
            Assert.Single(egress.FileResults);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "EgressService")]
        public async Task Start_Participant_Egress()
        {
            await roomClient.CreateRoom(new CreateRoomRequest { Name = TestConstants.ROOM_NAME });
            await fixture.PublishVideoTrackInRoom(
                roomClient,
                TestConstants.ROOM_NAME,
                TestConstants.PARTICIPANT_IDENTITY
            );
            var request = new ParticipantEgressRequest
            {
                RoomName = TestConstants.ROOM_NAME,
                Identity = TestConstants.PARTICIPANT_IDENTITY,
                FileOutputs =
                {
                    new EncodedFileOutput
                    {
                        FileType = EncodedFileType.Mp4,
                        Filepath = "/tmp/test.mp4",
                    },
                },
            };
            var egress = await egressClient.StartParticipantEgress(request);
            Assert.NotNull(egress);
            Assert.Equal(TestConstants.ROOM_NAME, egress.RoomName);
            Assert.Single(egress.FileResults);
            Assert.Equal(TestConstants.PARTICIPANT_IDENTITY, egress.Participant.Identity);
            Assert.Single(egress.Participant.FileOutputs);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "EgressService")]
        public async Task Start_Track_Egress()
        {
            await roomClient.CreateRoom(new CreateRoomRequest { Name = TestConstants.ROOM_NAME });
            await fixture.PublishVideoTrackInRoom(
                roomClient,
                TestConstants.ROOM_NAME,
                TestConstants.PARTICIPANT_IDENTITY
            );
            var participant = await roomClient.GetParticipant(
                new RoomParticipantIdentity
                {
                    Room = TestConstants.ROOM_NAME,
                    Identity = TestConstants.PARTICIPANT_IDENTITY,
                }
            );
            var videoTrack = participant
                .Tracks.Where(t => t.Type == TrackType.Video)
                .FirstOrDefault();
            Assert.NotNull(videoTrack);
            var request = new TrackEgressRequest
            {
                RoomName = TestConstants.ROOM_NAME,
                TrackId = videoTrack.Sid,
                File = new DirectFileOutput { Filepath = "{room_name}/{track_id}" },
            };
            var egress = await egressClient.StartTrackEgress(request);
            Assert.NotNull(egress);
            Assert.Equal(egress.Track.TrackId, videoTrack.Sid);
            Assert.Equal(TestConstants.ROOM_NAME, egress.Track.RoomName);
            Assert.Equal(TestConstants.ROOM_NAME, egress.RoomName);
            Assert.Single(egress.FileResults);
            Assert.Equal("{room_name}/{track_id}", egress.Track.File.Filepath);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "EgressService")]
        public async Task Start_Web_Egress()
        {
            var request = new WebEgressRequest
            {
                Url = "https://google.com",
                VideoOnly = true,
                FileOutputs =
                {
                    new EncodedFileOutput
                    {
                        FileType = EncodedFileType.Mp4,
                        Filepath = "/tmp/test.mp4",
                    },
                },
            };
            var egress = await egressClient.StartWebEgress(request);
            Assert.NotNull(egress);
            Assert.Single(egress.FileResults);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "EgressService")]
        public async Task Update_Layout()
        {
            await fixture.PublishVideoTrackInRoom(
                roomClient,
                TestConstants.ROOM_NAME,
                TestConstants.PARTICIPANT_IDENTITY
            );
            var request = new RoomCompositeEgressRequest { RoomName = TestConstants.ROOM_NAME };
            request.FileOutputs.Add(
                new EncodedFileOutput { FileType = EncodedFileType.Mp4, Filepath = "/tmp/test.mp4" }
            );
            var egress = await egressClient.StartRoomCompositeEgress(request);
            Assert.Equal("", egress.RoomComposite.Layout);
            // Wait until room composite egress is active
            var timeout = DateTime.Now.AddSeconds(10);
            while (
                egress != null
                && egress.Status != EgressStatus.EgressActive
                && DateTime.Now < timeout
            )
            {
                await Task.Delay(250);
                var egresses = await egressClient.ListEgress(
                    new ListEgressRequest { RoomName = TestConstants.ROOM_NAME }
                );
                egress = egresses.Items.Where(e => e.EgressId == egress.EgressId).FirstOrDefault();
            }
            Assert.NotNull(egress);
            Assert.Equal(EgressStatus.EgressActive, egress.Status);

            var newLayout = "single-speaker-light";
            var updateRequest = new UpdateLayoutRequest
            {
                EgressId = egress.EgressId,
                Layout = newLayout,
            };
            var updatedEgress = await egressClient.UpdateLayout(updateRequest);
            Assert.NotNull(updatedEgress);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "EgressService")]
        public async Task Stop_Egress()
        {
            await roomClient.CreateRoom(new CreateRoomRequest { Name = TestConstants.ROOM_NAME });
            var request = new RoomCompositeEgressRequest { RoomName = TestConstants.ROOM_NAME };
            request.FileOutputs.Add(
                new EncodedFileOutput { FileType = EncodedFileType.Mp4, Filepath = "/tmp/test.mp4" }
            );
            var egress = await egressClient.StartRoomCompositeEgress(request);
            var stopRequest = new StopEgressRequest { EgressId = egress.EgressId };
            var response = await egressClient.StopEgress(stopRequest);
            Assert.NotNull(response);
        }
    }
}
