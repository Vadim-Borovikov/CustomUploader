﻿using System;
using CustomUploader.Logic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CustomUploader.Tests
{
    [TestClass]
    public class DataManagerTests
    {
        [TestMethod]
        public void CreateFolderTest()
        {
            using (var provider = new DataManager("client_secret.json", ParentFolderId))
            {
                string id = provider.GetOrCreateFolder("Test");
                Console.WriteLine(id);
            }
        }

        [TestMethod]
        public void GetFolderTest()
        {
            using (var provider = new DataManager("client_secret.json", ParentFolderId))
            {
                string id = provider.GetOrCreateFolder("Test");
                Assert.AreEqual(FolderId, id);
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

        private static void UploadFileTest(string path)
        {
            using (var provider = new DataManager("client_secret.json", ParentFolderId))
            {
                bool success = provider.UploadFile(path, FolderId, 10, null);
                Assert.IsTrue(success);
            }
        }

        private const string ParentFolderId = "0B1IWpTUgXs1xaUI4bDA2WWFlSFU";
        private const string FolderId = "0B1IWpTUgXs1xZzBDUTV6eFBIT1U";

        private const string EmptyFilePath = "D:/Test/empty.txt";
        private const string TextFilePath = "D:/Test/text.txt";
        private const string AudioFilePath = "D:/Test/audio.mp3";
        private const string VideoFilePath = "D:/Test/video.mkv";
    }
}