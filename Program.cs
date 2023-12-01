using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace SQL_CONCAT_Fixer
{
    class Program
    {
        static string basepath = @"C:\devlab\teststudio\Wellness_firstLastNameUpdate\Databases\CPScreen\dbo\Stored Procedures\";


        static string[] excludes = new[] {
            "CheckZIPLocation.sql",   //too many functions
            "LabImportFillInRawDatFileName.sql",   // we dont do + at the end of line yet
            "selectohscustomersnapshot.sql",        // have not investigated
            "GetMVRRequestReportInfo.sql"   ,// not investigated
            "GetInstantTestOrderSiteDetailsByRequestorOrderId.sql"   //must investigate

        };



        static void Main(string[] args)
        {
            cclog.ClearLog();

            var files = makeFileList();
            var chgData = new ConcurrentBag<SqlConcatFileReplace>();
            Parallel.ForEach(files, file =>
            {
                var name = file.Name;
                if (!excludes.Contains(name))
                    chgData.Add(new SqlConcatFileReplace(file));
            });
            cclog.wlf("process files:");
            foreach (var file in chgData)
            {

                file.processFile();
            }
            cclog.wlf("Done processing");


            var srtd = chgData.OrderByDescending(i => i.totalUpdates);
            cclog.wlf($"Updates, Lines, Sproc File Name, FullFileName");

            foreach (var f in srtd)
            {
                cclog.wlf($"{f.totalUpdates}, {f.totalLines},{f._filename},{f.fi.FullName} ");
            }
        }
        public static new List<FileInfo> makeFileList()
        {
            var files = new List<FileInfo>();
            var dirs = new DirectoryInfo(basepath).GetDirectories().ToList();
            if (dirs.Count() == 0)
            {
                dirs = new List<DirectoryInfo> { new DirectoryInfo(basepath) };
            }

            dirs.ForEach(d =>
            {
                files.AddRange(d.GetFiles().ToList());
            });
            return files;

        }


        public static void ProcessFile()
        {
            cclog.ClearLog();


            var files = new[] {
                new FileInfo(
            //"CORE_SelectTestResultsResellerSearch.sql",
            //"CORE_SelectTestResultsSearch.sql",
           @"C:\devlab\teststudio\EasyWellness\Databases\CPScreen\dbo\Stored Procedures\SelectP-Sz\"+
           //"t2.sql"
"spGenerateGenericEmailConfirmation.sql")
            //    "CORE_SelectTestResults.sql",
            //    "CORE_SelectTestResults_CPR.sql",
            //    "CORE_SelectTestResultsByCustAvailDate.sql",
            //    "CORE_SelectTestResultsReseller.sql",
            //    "CORE_SelectTestResultsResellerByCustAvailDate.sql"
            };
            ProcessFiles(files);
        }
        public static void ProcessFiles(FileInfo[] files)
        {

            var chgData = new ConcurrentBag<SqlConcatFileReplace>();
            Parallel.ForEach(files, file =>
            {
                chgData.Add(new SqlConcatFileReplace(file));
            });
            cclog.wlf("process files:");
            foreach (var file in chgData)
            //                Parallel.ForEach(chgData, file =>
            {
                file.processFile();
            }
            cclog.wlf("Done processing");


            var srtd = chgData.OrderByDescending(i => i.totalUpdates);
            cclog.wlf($"Updates, Lines, Sproc File Name, FullFileName");

            foreach (var f in srtd)
            {
                cclog.wlf($"{f.totalUpdates}, {f.totalLines},{f._filename},{f.fi.FullName} ");
            }



        }
    }



}
