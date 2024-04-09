﻿#if NET472_OR_GREATER || NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
#endif
using FluentAssertions;
using Mscc.GenerativeAI;
using Xunit;
using Xunit.Abstractions;

namespace Test.Mscc.GenerativeAI
{
    [Collection(nameof(ConfigurationFixture))]
    public class GoogleAi_Gemini15Pro_Should
    {
        private readonly ITestOutputHelper _output;
        private readonly ConfigurationFixture _fixture;
        private readonly string _model = Model.Gemini15ProLatest;

        public GoogleAi_Gemini15Pro_Should(ITestOutputHelper output, ConfigurationFixture fixture)
        {
            _output = output;
            _fixture = fixture;
        }

        [Fact]
        public void Initialize_Gemini15Pro()
        {
            // Arrange

            // Act
            var model = new GenerativeModel(apiKey: _fixture.ApiKey, model: _model);

            // Assert
            model.Should().NotBeNull();
            model.Name.Should().Be(Model.Gemini15Pro.SanitizeModelName());
        }

        [Fact]
        public async void GenerateContent_WithInvalidAPIKey_ChangingBeforeRequest()
        {
            // Arrange
            var prompt = "Tell me 4 things about Taipei. Be short.";
            var googleAI = new GoogleAI(apiKey: "WRONG_API_KEY");
            var model = googleAI.GenerativeModel(model: _model);
            model.ApiKey = _fixture.ApiKey;

            // Act
            var response = await model.GenerateContent(prompt);

            // Assert
            response.Should().NotBeNull();
            response.Candidates.Should().NotBeNull().And.HaveCount(1);
            response.Text.Should().NotBeEmpty();
            _output.WriteLine(response?.Text);
        }

        [Fact]
        public async void GenerateContent_WithInvalidAPIKey_ChangingAfterRequest()
        {
            // Arrange
            var prompt = "Tell me 4 things about Taipei. Be short.";
            var googleAI = new GoogleAI(apiKey: "WRONG_API_KEY");
            var model = googleAI.GenerativeModel(model: _model);
            await Assert.ThrowsAsync<HttpRequestException>(() => model.GenerateContent(prompt));

            // Act
            model.ApiKey = _fixture.ApiKey;
            var response = await model.GenerateContent(prompt);

            // Assert
            response.Should().NotBeNull();
            response.Candidates.Should().NotBeNull().And.HaveCount(1);
            response.Text.Should().NotBeEmpty();
            _output.WriteLine(response?.Text);
        }

        [Fact]
        public async void GenerateContent_Using_JsonMode()
        {
            // Arrange
            var prompt = "List a few popular cookie recipes using this JSON schema: {'type': 'object', 'properties': { 'recipe_name': {'type': 'string'}}}";
            var googleAI = new GoogleAI(apiKey: _fixture.ApiKey);
            var model = googleAI.GenerativeModel(model: _model);
            model.UseJsonMode = true;

            // Act
            var response = await model.GenerateContent(prompt);

            // Assert
            response.Should().NotBeNull();
            response.Candidates.Should().NotBeNull().And.HaveCount(1);
            response.Text.Should().NotBeEmpty();
            _output.WriteLine(response?.Text);
        }

        [Fact]
        public async void Generate_Text_From_Image()
        {
            // Arrange
            var model = new GenerativeModel(apiKey: _fixture.ApiKey, model: _model);
            var request = new GenerateContentRequest { Contents = new List<Content>() };
            var base64image = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";
            var parts = new List<IPart>
            {
                new TextData { Text = "What is this picture about?" },
                new InlineData { MimeType = "image/jpeg", Data = base64image }
            };
            request.Contents.Add(new Content { Role = Role.User, Parts = parts });

            // Act
            var response = await model.GenerateContent(request);

            // Assert
            response.Should().NotBeNull();
            response.Candidates.Should().NotBeNull().And.HaveCount(1);
            response.Candidates.FirstOrDefault().Content.Should().NotBeNull();
            response.Candidates.FirstOrDefault().Content.Parts.Should().NotBeNull().And.HaveCountGreaterThanOrEqualTo(1);
            response.Text.Should().Contain("red");
            _output.WriteLine(response?.Text);
        }

        [Fact]
        public async void Describe_Image_From_InlineData()
        {
            // Arrange
            var prompt = "Parse the time and city from the airport board shown in this image into a list, in Markdown";
            var model = new GenerativeModel(apiKey: _fixture.ApiKey, model: _model);
            // Images
            var board = await TestExtensions.ReadImageFileBase64Async("https://ai.google.dev/static/docs/images/timetable.png");
            var request = new GenerateContentRequest(prompt);
            request.Contents[0].Parts.Add(
                new InlineData { MimeType = "image/png", Data = board }
            );

            // Act
            var response = await model.GenerateContent(request);

            // Assert
            response.Should().NotBeNull();
            response.Candidates.Should().NotBeNull().And.HaveCount(1);
            response.Candidates.FirstOrDefault().Content.Should().NotBeNull();
            response.Candidates.FirstOrDefault().Content.Parts.Should().NotBeNull().And.HaveCountGreaterThanOrEqualTo(1);
            _output.WriteLine(response?.Text);
        }

        [Theory]
        [InlineData("scones.jpg", "image/jpeg", "What is this picture?", "blueberries")]
        [InlineData("cat.jpg", "image/jpeg", "Describe this image", "snow")]
        [InlineData("cat.jpg", "image/jpeg", "Is it a cat?", "Yes")]
        //[InlineData("animals.mp4", "video/mp4", "What's in the video?", "Zootopia")]
        public async void Generate_Text_From_ImageFile(string filename, string mimetype, string prompt, string expected)
        {
            // Arrange
            var model = new GenerativeModel(apiKey: _fixture.ApiKey, model: _model);
            var base64image = Convert.ToBase64String(File.ReadAllBytes(Path.Combine(Environment.CurrentDirectory, "payload", filename)));
            var parts = new List<IPart>
            {
                new TextData { Text = prompt },
                new InlineData { MimeType = mimetype, Data = base64image }
            };
            var generationConfig = new GenerationConfig()
            {
                Temperature = 0.4f, TopP = 1, TopK = 32, MaxOutputTokens = 1024
            };

            // Act
            var response = await model.GenerateContent(parts, generationConfig);

            // Assert
            response.Should().NotBeNull();
            response.Candidates.Should().NotBeNull().And.HaveCount(1);
            response.Candidates.FirstOrDefault().Content.Should().NotBeNull();
            response.Candidates.FirstOrDefault().Content.Parts.Should().NotBeNull().And.HaveCountGreaterThanOrEqualTo(1);
            response.Text.Should().Contain(expected);
            _output.WriteLine(response?.Text);
        }

        [Theory]
        [InlineData("scones.jpg", "What is this picture?", "blueberries")]
        [InlineData("cat.jpg", "Describe this image", "snow")]
        [InlineData("cat.jpg", "Is it a feline?", "Yes")]
        //[InlineData("animals.mp4", "video/mp4", "What's in the video?", "Zootopia")]
        public async void Describe_AddMedia_From_ImageFile(string filename, string prompt, string expected)
        {
            // Arrange
            var model = new GenerativeModel(apiKey: _fixture.ApiKey, model: _model);
            var request = new GenerateContentRequest(prompt)
            {
                GenerationConfig = new GenerationConfig()
                {
                    Temperature = 0.4f, TopP = 1, TopK = 32, MaxOutputTokens = 1024
                }
            };
            request.AddMedia(Path.Combine(Environment.CurrentDirectory, "payload", filename));

            // Act
            var response = await model.GenerateContent(request);

            // Assert
            response.Should().NotBeNull();
            response.Candidates.Should().NotBeNull().And.HaveCount(1);
            response.Candidates.FirstOrDefault().Content.Should().NotBeNull();
            response.Candidates.FirstOrDefault().Content.Parts.Should().NotBeNull().And.HaveCountGreaterThanOrEqualTo(1);
            response.Text.Should().Contain(expected);
            _output.WriteLine(response?.Text);
        }

        [Fact]
        public async void Describe_AddMedia_From_Url()
        {
            // Arrange
            var prompt = "Parse the time and city from the airport board shown in this image into a list, in Markdown";
            var model = new GenerativeModel(apiKey: _fixture.ApiKey, model: _model);
            var request = new GenerateContentRequest(prompt);
            await request.AddMedia("https://ai.google.dev/static/docs/images/timetable.png");

            // Act
            var response = await model.GenerateContent(request);

            // Assert
            response.Should().NotBeNull();
            response.Candidates.Should().NotBeNull().And.HaveCount(1);
            response.Candidates.FirstOrDefault().Content.Should().NotBeNull();
            response.Candidates.FirstOrDefault().Content.Parts.Should().NotBeNull().And.HaveCountGreaterThanOrEqualTo(1);
            _output.WriteLine(response?.Text);
        }

        [Fact]
        public async void Describe_AddMedia_From_UrlRemote()
        {
            // Arrange
            var prompt = "Parse the time and city from the airport board shown in this image into a list, in Markdown";
            var model = new GenerativeModel(apiKey: _fixture.ApiKey, model: _model);
            var request = new GenerateContentRequest(prompt);
            await request.AddMedia("https://ai.google.dev/static/docs/images/timetable.png", useOnline: true);

            // Act
            var response = await model.GenerateContent(request);

            // Assert
            response.Should().NotBeNull();
            response.Candidates.Should().NotBeNull().And.HaveCount(1);
            response.Candidates.FirstOrDefault().Content.Should().NotBeNull();
            response.Candidates.FirstOrDefault().Content.Parts.Should().NotBeNull().And.HaveCountGreaterThanOrEqualTo(1);
            _output.WriteLine(response?.Text);
        }

        [Theory]
        [InlineData("scones.jpg", "Set of blueberry scones")]
        [InlineData("cat.jpg", "Wildcat on snow")]
        [InlineData("cat.jpg", "Cat in the snow")]
        [InlineData("animals.mp4", "Zootopia in da house")]
        public async void Upload_File_Using_FileAPI(string filename, string displayName)
        {
            // Arrange
            var filePath = Path.Combine(Environment.CurrentDirectory, "payload", filename);
            IGenerativeAI genAi = new GoogleAI(_fixture.ApiKey);
            var model = genAi.GenerativeModel(_model);
            
            // Act
            var response = await model.UploadMedia(filePath, displayName);
            
            // Assert
            response.Should().NotBeNull();
            response.File.Should().NotBeNull();
            response.File.Name.Should().NotBeNull();
            // response.File.DisplayName.Should().Be(displayName);
            // response.File.MimeType.Should().Be("image/jpeg");
            // response.File.CreateTime.Should().BeGreaterThan(DateTime.Now.Add(TimeSpan.FromHours(48)));
            // response.File.ExpirationTime.Should().NotBeNull();
            // response.File.UpdateTime.Should().NotBeNull();
            response.File.SizeBytes.Should().BeGreaterThan(0);
            response.File.Sha256Hash.Should().NotBeNull();
            response.File.Uri.Should().NotBeNull();
            _output.WriteLine(response?.File.Uri);
        }

        [Fact]
        public async void List_Files()
        {
            // Arrange
            IGenerativeAI genAi = new GoogleAI(_fixture.ApiKey);
            var model = genAi.GenerativeModel(_model);

            // Act
            var sut = await model.ListFiles();

            // Assert
            sut.Should().NotBeNull();
            sut.Should().NotBeNull().And.HaveCountGreaterThanOrEqualTo(1);
            sut.ForEach(x =>
            {
                _output.WriteLine($"File: {x.Name} (MimeType: {x.MimeType}, Size: {x.SizeBytes} bytes, Created: {x.CreateTime} UTC, Updated: {x.UpdateTime} UTC)");
                _output.WriteLine(($@"Uri: {x.Uri}"));
            });
        }

        [Theory]
        [InlineData("files/e0zb8cooleen")]
        [InlineData("5kfit2vb3r9e")]
        [InlineData("files/s0ebp56ef0ri")]
        [InlineData("e8dz3lhkyu7w")]
        [InlineData("bb1e4cqfk6wc")]
        public async void Get_File(string fileName)
        {
            // Arrange
            IGenerativeAI genAi = new GoogleAI(_fixture.ApiKey);
            var model = genAi.GenerativeModel(_model);
            var files = await model.ListFiles();
            // var fileName = files.FirstOrDefault().Name;

            // Act
            var sut = await model.GetFile(fileName);

            // Assert
            sut.Should().NotBeNull();
            _output.WriteLine($"File: {sut.Name} (MimeType: {sut.MimeType}, Size: {sut.SizeBytes} bytes, Created: {sut.CreateTime} UTC, Updated: {sut.UpdateTime} UTC)");
            _output.WriteLine(($@"Uri: {sut.Uri}"));
        }
        
        [Fact]
        public async void Delete_File()
        {
            // Arrange
            IGenerativeAI genAi = new GoogleAI(_fixture.ApiKey);
            var model = genAi.GenerativeModel(_model);
            var files = await model.ListFiles();
            var fileName = files.FirstOrDefault().Name;
            _output.WriteLine($"File: {fileName}");
            
            // Act
            var response = await model.DeleteFile(fileName);
            
            // Assert
            response.Should().NotBeNull();
            _output.WriteLine(response);
        }

        [Fact]
        public async void Describe_Single_Media_From_FileAPI()
        {
            // Arrange
            var prompt = "Describe the image with a creative description";
            IGenerativeAI genAi = new GoogleAI(_fixture.ApiKey);
            var model = genAi.GenerativeModel(_model);
            var request = new GenerateContentRequest(prompt);
            var files = await model.ListFiles();
            var file = files.Where(x => x.MimeType.StartsWith("image/")).FirstOrDefault();
            _output.WriteLine($"File: {file.Name}");
            request.AddMedia(file);

            // Act
            var response = await model.GenerateContent(request);

            // Assert
            response.Should().NotBeNull();
            response.Candidates.Should().NotBeNull().And.HaveCount(1);
            response.Candidates.FirstOrDefault().Content.Should().NotBeNull();
            response.Candidates.FirstOrDefault().Content.Parts.Should().NotBeNull().And.HaveCountGreaterThanOrEqualTo(1);
            _output.WriteLine(response?.Text);
        }

        [Fact]
        public async void Describe_Images_From_FileAPI()
        {
            // Arrange
            var prompt = "Make a short story from the media resources. The media resources are:";
            IGenerativeAI genAi = new GoogleAI(_fixture.ApiKey);
            var model = genAi.GenerativeModel(_model);
            var request = new GenerateContentRequest(prompt);
            var files = await model.ListFiles();
            foreach (var file in files.Where(x => x.MimeType.StartsWith("image/")))
            {
                _output.WriteLine($"File: {file.Name}");
                request.AddMedia(file);
            }

            // Act
            var response = await model.GenerateContent(request);

            // Assert
            response.Should().NotBeNull();
            response.Candidates.Should().NotBeNull().And.HaveCount(1);
            response.Candidates.FirstOrDefault().Content.Should().NotBeNull();
            response.Candidates.FirstOrDefault().Content.Parts.Should().NotBeNull().And.HaveCountGreaterThanOrEqualTo(1);
            _output.WriteLine(response?.Text);
        }

        [Fact(Skip = "Bad Request due to FileData part")]
        // [Fact]
        public async void Describe_Videos_From_FileAPI()
        {
            // Arrange
            var prompt = "Make a short story from the media resources. The media resources are:";
            IGenerativeAI genAi = new GoogleAI(_fixture.ApiKey);
            var model = genAi.GenerativeModel(_model);
            var request = new GenerateContentRequest(prompt);
            var files = await model.ListFiles();
            foreach (var file in files.Where(x => x.MimeType.StartsWith("video/")))
            {
                _output.WriteLine($"File: {file.Name}");
                request.AddMedia(file);
            }

            // Act
            var response = await model.GenerateContent(request);

            // Assert
            response.Should().NotBeNull();
            response.Candidates.Should().NotBeNull().And.HaveCount(1);
            response.Candidates.FirstOrDefault().Content.Should().NotBeNull();
            response.Candidates.FirstOrDefault().Content.Parts.Should().NotBeNull().And.HaveCountGreaterThanOrEqualTo(1);
            _output.WriteLine(response?.Text);
        }
        
        [Fact]
        public async void Describe_Image_From_StorageBucket()
        {
            // Arrange
            var prompt = "Describe the image with a creative description";
            var model = new GenerativeModel(apiKey: _fixture.ApiKey, model: _model);
            var generationConfig = new GenerationConfig
            {
                Temperature = 0.4f,
                TopP = 1,
                TopK = 32,
                MaxOutputTokens = 2048
            };
            // var request = new GenerateContentRequest(prompt, generationConfig);
            var request = new GenerateContentRequest(prompt);
            request.Contents[0].Parts.Add(new FileData
            {
                FileUri = "gs://generativeai-downloads/images/scones.jpg",
                MimeType = "image/jpeg"
            });

            // Act
            var response = await model.GenerateContent(request);

            // Assert
            response.Should().NotBeNull();
            response.Candidates.Should().NotBeNull().And.HaveCount(1);
            response.Candidates.FirstOrDefault().Content.Should().NotBeNull();
            response.Candidates.FirstOrDefault().Content.Parts.Should().NotBeNull().And.HaveCountGreaterThanOrEqualTo(1);
            _output.WriteLine(response?.Text);
        }

        [Fact(Skip = "Bad Request due to FileData part")]
        public async void Describe_Image_From_FileData()
        {
            // Arrange
            var prompt = "Parse the time and city from the airport board shown in this image into a list, in Markdown";
            var model = new GenerativeModel(apiKey: _fixture.ApiKey, model: _model);
            var request = new GenerateContentRequest(prompt);
            request.Contents[0].Parts.Add(new FileData
            {
                FileUri = "https://ai.google.dev/static/docs/images/timetable.png",
                MimeType = "image/png"
            });

            // Act
            var response = await model.GenerateContent(request);

            // Assert
            response.Should().NotBeNull();
            response.Candidates.Should().NotBeNull().And.HaveCount(1);
            response.Candidates.FirstOrDefault().Content.Should().NotBeNull();
            response.Candidates.FirstOrDefault().Content.Parts.Should().NotBeNull().And.HaveCountGreaterThanOrEqualTo(1);
            _output.WriteLine(response?.Text);
        }

        [Fact(Skip = "URL scheme not supported")]
        public async void Multimodal_Video_Input()
        {
            // Arrange
            var model = new GenerativeModel(apiKey: _fixture.ApiKey, model: _model);
            var video = await TestExtensions.ReadImageFileBase64Async("gs://cloud-samples-data/video/animals.mp4");
            var request = new GenerateContentRequest("What's in the video?");
            request.Contents[0].Role = Role.User;
            request.Contents[0].Parts.Add(new InlineData { MimeType = "video/mp4", Data = video });

            // Act
            var response = await model.GenerateContent(request);

            // Assert
            response.Should().NotBeNull();
            response.Candidates.Should().NotBeNull().And.HaveCount(1);
            response.Candidates.FirstOrDefault().Content.Should().NotBeNull();
            response.Candidates.FirstOrDefault().Content.Parts.Should().NotBeNull().And.HaveCountGreaterThanOrEqualTo(1);
            response.Text.Should().Contain("Zootopia");
            _output.WriteLine(response?.Text);
        }
    }
}
