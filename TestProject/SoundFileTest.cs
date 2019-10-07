using auxmic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using NAudio.Wave;
using System.Collections.Generic;
using System.Linq;

namespace TestProject
{
    
    
    /// <summary>
    ///This is a test class for SoundFileTest and is intended
    ///to contain all SoundFileTest Unit Tests
    ///</summary>
    [TestClass()]
    public class SoundFileTest
    {
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for WaveData
        ///</summary>
        [TestMethod()]
        public void WaveData_dtmf_1to0_Test()
        {
            string filename = @"..\..\..\TestProject\Data\dtmf_1to0.wav";

            Int32[] actual;
            Int32[] expected;

            ReadFile(filename, out actual, out expected, TestData.dtmf_1to0);

            CollectionAssert.AreEqual(expected, actual);
        }

        private static void ReadFile(string filename, out Int32[] actual, out Int32[] expected, Int32[] fileData)
        {
            WaveFormat resampleFormat = null;
            SoundFile soundFile = new SoundFile(filename, resampleFormat);

            List<Int32> leftChannel = new List<Int32>();

            int L = 256; //_syncParams.L;
            Int32[] data; // SoundFile.WaveData.LeftChannel;
            int N = soundFile.DataLength;

            for (int i = 0; i <= N - L; i += L)
            {
                data = soundFile.Read(L);
                leftChannel.AddRange(data);
            }

            actual = leftChannel.ToArray();

            expected = fileData.Take(fileData.Length - fileData.Length % L).ToArray();
        }

        [TestMethod()]
        public void WaveData_DSC_6785_48kHz_16bit_mono_Test()
        {
            string filename = @"..\..\..\TestProject\Data\DSC_6785_48kHz_16bit_mono.wav";

            Int32[] actual;
            Int32[] expected;

            ReadFile(filename, out actual, out expected, TestData.DSC_6785_48kHz_16bit_mono);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void WaveData_master_48kHz_16bit_stereo_Test()
        {
            string filename = @"..\..\..\TestProject\Data\master_48kHz_16bit_stereo.wav";

            Int32[] actual;
            Int32[] expected;

            ReadFile(filename, out actual, out expected, TestData.master_48kHz_16bit_stereo);

            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
