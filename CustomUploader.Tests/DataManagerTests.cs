using System;
using System.IO;
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

        private static void UploadFileTest(string path)
        {
            using (var provider = new DataManager("client_secret.json", ParentFolderId, null))
            {
                long? id = provider.UploadFile(new FileInfo(path), FolderId, 10, null);
                Assert.IsNotNull(id);
            }
        }

        private const string ParentFolderId = "0B1IWpTUgXs1xaUI4bDA2WWFlSFU";
        private const string FolderId = "1uPh_peyzr1JYEGuWwvBGxOOHaBvotzpq";

        private const string EmptyFilePath = "D:/Test/empty.txt";
        private const string TextFilePath = "D:/Test/text.txt";
        private const string AudioFilePath = "D:/Test/audio.mp3";
    }
}