using System;
using System.Collections.Generic;
using System.Linq;
using CustomUploader.Logic;
using Google.Apis.Drive.v3.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CustomUploader.LogicTests1
{
    [TestClass]
    public class GoogleApisDriveProviderTests
    {
        [TestMethod]
        public void GoogleApisDriveProviderCtorTest()
        {
            // ReSharper disable once UnusedVariable
            using (var provider = new GoogleApisDriveProvider("client_secret.json"))
            {
            }
        }

        [TestMethod]
        public void GetFolderIdsByNameTest()
        {
            using (var provider = new GoogleApisDriveProvider("client_secret.json"))
            {
                List<File> ids = provider.GetFolderIdsByName("Test");
                Assert.AreEqual(1, ids.Count);
                Console.WriteLine(ids.Single().Id);
            }
        }

        [TestMethod]
        public void UploadEmptyFileTest()
        {
            UploadFileTest(EmptyFilePath);
        }

        [TestMethod]
        public void UploadTextFileTest()
        {
            UploadFileTest(TextFilePath);
        }

        [TestMethod]
        public void UploadAudioFileTest()
        {
            UploadFileTest(AudioFilePath);
        }

        [TestMethod]
        public void UploadVideoFileTest()
        {
            UploadFileTest(VideoFilePath);
        }

        [TestMethod]
        public void CreateFolderTest()
        {
            using (var provider = new GoogleApisDriveProvider("client_secret.json"))
            {
                const string Name = "Test";
                File folder = provider.CreateFolder(Name, ParentFolderId).Result;
                Assert.IsNotNull(folder);
                Assert.AreEqual(folder.Name, Name);
            }
        }

        private static void UploadFileTest(string path)
        {
            using (var provider = new GoogleApisDriveProvider("client_secret.json"))
            {
                bool success = provider.Upload(path, FolderId, 10).Result;
                Assert.IsTrue(success);
            }
        }

        private const string ParentFolderId = "0B1IWpTUgXs1xaUI4bDA2WWFlSFU";
        private const string FolderId = "0B1IWpTUgXs1xdExBdXJfbThCRTg";

        private const string EmptyFilePath = "D:/Test/empty.txt";
        private const string TextFilePath = "D:/Test/text.txt";
        private const string AudioFilePath = "D:/Test/audio.mp3";
        private const string VideoFilePath = "D:/Test/video.mkv";
    }
}