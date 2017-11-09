using System;
using System.Collections.Generic;
using System.IO;
using CustomUploader.Logic;
using CustomUploader.Logic.Timepad.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CustomUploader.Tests
{
    [TestClass]
    public class DataManagerTests
    {
        [TestMethod]
        public void CreateFolderTest()
        {
            using (var dataManager = new DataManager("client_secret.json", ParentFolderId, null))
            {
                string id = dataManager.GetOrCreateFolder("Test");
                Console.WriteLine(id);
            }
        }

        [TestMethod]
        public void GetFolderTest()
        {
            using (var dataManager = new DataManager("client_secret.json", ParentFolderId, null))
            {
                string id = dataManager.GetOrCreateFolder("Test");
                Assert.AreEqual(FolderId, id);
            }
        }

        [TestMethod]
        public void GetTimepadEvents()
        {
            List<Event> events = DataManager.GetTimepadEvents(53244, DateTime.Now.AddDays(-7), DateTime.Now);
            Assert.IsNotNull(events);
            Assert.AreNotSame(0, events.Count);
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

        private static void UploadFileTest(string path)
        {
            using (var provider = new DataManager("client_secret.json", ParentFolderId, null))
            {
                long? id = provider.UploadFile(new FileInfo(path), FolderId, 10, null);
                Assert.IsNotNull(id);
            }
        }

        private const string ParentFolderId = "0B1IWpTUgXs1xaUI4bDA2WWFlSFU";
        private const string FolderId = "1u7TF8ZtLdHsT1jwesSNrWTngvGB0yFge";

        private const string EmptyFilePath = "D:/Test/empty.txt";
        private const string TextFilePath = "D:/Test/text.txt";
        private const string AudioFilePath = "D:/Test/audio.mp3";
        private const string VideoFilePath = "D:/Test/video.mkv";
    }
}