using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace SQL_CONCAT_Fixer
{
    class Program
    {
        static void Main(string[] args)
        {
            cclog.ClearLog();

            var files = new[] {
            "CORE_SelectTestResultsResellerSearch.sql",
            "CORE_SelectTestResultsSearch.sql",
            "CORE_SelectTestResults.sql",
            "CORE_SelectTestResults_CPR.sql",
            "CORE_SelectTestResultsByCustAvailDate.sql",
            "CORE_SelectTestResultsReseller.sql",
            "CORE_SelectTestResultsResellerByCustAvailDate.sql"
            };
            ConcatFixer.totalLines = 0;
            ConcatFixer.totalUpdates = 0;
            foreach (var file in files)
            {
                var c = new ConcatFixer(file);


            };
            cclog.wlf("totalLines:" + ConcatFixer.totalLines);
            cclog.wlf("totalUpdates:" + ConcatFixer.totalUpdates);

            //var file = "CORE_SelectTestResults.sql";

        }
    }


    public class ConcatFixer
    {
        public static int cntr { get; set; }
        public static int totalLines { get; set; }
        public static int totalUpdates { get; set; }

        public string _filename { get; set; }
        public string opath =
        //@"C:\devlab\bfrsql\";
        "C:\\devlab\\teststudio\\EasyWellness\\Databases\\CPScreen\\dbo\\Stored Procedures\\A-D\\";
        public string updatePath = @"C:\devlab\UpdatedSql\";
        public ConcatFixer(string fileName)
        {
            _filename = fileName;
            cntr = 0;
            processFile();
            //  cclog.wlf("cntr:" + cntr);
        }

        public void processFile()
        {
            var lines = File.ReadAllLines(opath + _filename);

            cclog.wlf(" ");
            cclog.wlf("Processing file: " + _filename + " : lines: " + lines.Length);
            var chgLines = 0;
            for (var inx = 0; inx < lines.Length; inx++)
            {

                totalLines++;

                // var line = lines[inx];
                // line = line.Replace("'''", "'~'");
                var replaceLine = DoParmsInParmsAndProcess(lines[inx], inx);
                if (replaceLine != lines[inx])
                {
                    // report the line update
                    cclog.wlf("");

                    cclog.wlf($"Line: {inx}  bfr: {lines[inx]} ");
                    cclog.wlf($"Line: {inx}  aft: {replaceLine} ");

                    lines[inx] = replaceLine;
                    //  break;

                }

            }


            var backFile = opath + _filename.Replace(".sql", ".bak");
            if (File.Exists(backFile)) File.Delete(backFile);
            if (File.Exists(updatePath + _filename)) File.Delete(updatePath + _filename);

            // these two lines make the backup and replace the original
            File.Move(opath + _filename, backFile);
            File.WriteAllLines(opath + _filename, lines);

        }

        public static void writeUpdated(string fname, string[] lines)
        {


        }



        public static string DoParmsInParmsAndProcess(string line, int inx)
        {
            var ret = "";
            if (line.IndexOf("+") < 0) return line;
            cntr++;
            totalUpdates++;

            if (line.IndexOf("(") > 0)
            {
                // oh no there is a paren lets get the inners
                var strtInx = line.IndexOf("(") + 1;

                var strt = line.Substring(strtInx);

                var endinx = strt.LastIndexOf(")");
                if (endinx <= 0)
                {
                    // the parms here have split lines so we can just try to process
                    ret = spllitSpecials_and_processLine(line, inx);
                }
                else
                {



                    var theInner = strt.Substring(0, endinx);

                    var b = line.Substring(0, strtInx);
                    var m = theInner;
                    var e = strt.Substring(endinx);


                    // if we have a funtion
                    //if (b[b.Length] == '(')
                    //{ 


                    //}


                    //  cclog.wlf($"b: {b}");
                    // cclog.wlf($"m: {m}");
                    //   cclog.wlf($"e: {e}");
                    ret = DoParmsInParmsAndProcess(b, inx) + DoParmsInParmsAndProcess(theInner, inx) + DoParmsInParmsAndProcess(e, inx);
                }
                ret = spllitSpecials_and_processLine(ret, inx);
            }
            else
            {
                ret = spllitSpecials_and_processLine(line, inx);
            }

            // cclog.wlf($"ret: {ret}");
            return ret;


        }





        public static string spllitSpecials_and_processLine(string line, int lineinx)
        {
            var ret = "";
            //  cclog.wlf("rslt:" + line);

            Dictionary<string, string> qph = new Dictionary<string, string>();

            var noquoteLine = "";

            //var frst = prts[0].TrimEnd();
            var insideQuote = false;
            var insideParen = false;
            var phraseInx = 0;
            var currentQuote = "";
            var firstQuote = false;
            for (var linx = 0; linx < line.Length; linx++)
            {
                var ltr = line[linx];

                if (!insideQuote)
                {
                    if (ltr == '\'')
                    {
                        // this is the start of a quote
                        insideQuote = true;
                        currentQuote = "" + ltr;
                        noquoteLine += $" ^{phraseInx}^ ";
                    }
                    else
                    {
                        noquoteLine += ltr;

                    }
                }
                else
                {
                    // we are inside a quote so we need to ignore two in a row
                    var ltr2 = ' ';
                    if (linx < line.Length - 1)
                    {
                        ltr2 = line[linx + 1];
                    }
                    if ((ltr == '\'') && (ltr2 == '\'') && !firstQuote)
                    {
                        // this is the second quote of a pair in a line
                        firstQuote = true;
                        currentQuote += ltr;
                        // linx = linx - 1;
                        continue;
                    }

                    if (ltr == '\'' && firstQuote)
                    {
                        // this is the second quote of a pair in a line
                        firstQuote = false;
                        currentQuote += ltr;
                        // linx = linx - 1;
                        continue;
                    }
                    else
                    {
                        if (ltr == '\'')
                        {
                            insideQuote = false;
                            // this is the end of the quote 
                            // put the quote in the collection 
                            currentQuote += ltr;
                            qph[$"^{phraseInx++}^"] = currentQuote;

                        }
                        else
                        {
                            currentQuote += ltr;

                        }
                    }


                }
            }
            qph[$"^{phraseInx++}^"] = currentQuote;


            // Here we should have a line with the quoted contents replaced by ^1^
            // 
            //  no specials that are inside quotes will be changed by further processing
            //  so here lets get rid of the instances of LIKE



            var ln = noquoteLine.Replace(" like ", "~");
            ln = ln.Replace(" LIKE ", "~");

            // now lets put the quote strings back in place

            var rebuiltLine = "";


            // the quote replacement was originally here but moved it below to lower risk
            // editing lines of dynamic sql


            var prts = ln.Split('~');
            for (var inx = 0; inx < prts.Length; inx++)
            {
                prts[inx] = processLine(prts[inx], lineinx);
            }

            ln = string.Join(" LIKE ", prts);
            // cclog.wlf("Likes:"+ln);


            // before putting the quotes back lets scrub extra spaces that we added
            // first remove all double spaces
            while (ln.IndexOf("  ") > 0)
            {
                ln = ln.Replace("  ", " ");
            }

            rebuiltLine = "";
            // rebuild the line by putting the quoted lines back into the line
            var nextEnd = -1;
            var nextStrt = 0;
            nextStrt = ln.IndexOf("^");
            while (nextStrt > 0)
            {
                rebuiltLine += ln.Substring(nextEnd + 1, nextStrt - 1 - nextEnd);
                nextEnd = ln.IndexOf("^", nextStrt + 1);

                var quoteId = ln.Substring(nextStrt, nextEnd - nextStrt + 1);
                //  cclog.wlf("quid:" + quoteId);
                rebuiltLine += qph[quoteId];


                nextStrt = ln.IndexOf("^", nextEnd + 1);
            }
            rebuiltLine += ln.Substring(nextEnd + 1);
            return rebuiltLine;
        }


        public static string processLine(string line, int inx)
        {

            // if the line comming in starts as a function return the line

            if (line.Length == 0) return "";

            if (line[0] == ')')
            {
                return line;
            }
            if (line[line.Length - 1] == '(')
            {
                return line;
            }



            var ret = line;
            if (line.IndexOf("+") > 0)
            {
                //   cclog.wlf("");

                //     cclog.wlf($"Line: {inx}  bfr: {line} ");
                //cclog.wlf($"Line: {inx}  aft: {replaceLine} ");
                // any line can have a this + this + this clause  
                // our goal is first to find the beginning and end of that clasue
                // then to replace the + with commas
                // then to put the new string inside a 'concat()'

                // find the first part to concat
                //
                // cant think of best way so do it the hard way to get going
                var strtInx = getIndexOfStart(line);
                var endInx = getIndexOfEnd(line);

                var startPart = line.Substring(0, strtInx);
                var endPart = "";
                if (endInx != line.Length)
                {
                    endPart = line.Substring(endInx);
                }


                var midPart = line.Substring(strtInx, endInx - strtInx);
                //  cclog.wlf("");

                //  cclog.wlf($"strt: {startPart}");
                //  cclog.wlf($"midPart: {midPart}");
                //  cclog.wlf($"endPart: {endPart}");


                ret = $"{startPart} {makeConcat(midPart, inx)} {endPart}";

            }
            return ret;
        }
        public static string makeConcat(string line, int x)
        {
            var ret = line;

            var prts = line.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
            // test for any blank spaces
            if (prts.Length == 1) return line;
            for (var inx = 0; inx < prts.Length; inx++)
            {
                prts[inx] = prts[inx].Trim();

                //if (prt.Trim().Length == 0)
                //{
                //  return line;
                // }
            }

            var comaLine = string.Join(",", prts);
            ret = $"concat({comaLine})";


            return ret;

        }

        public static int getIndexOfStart(string line)
        {
            var ret = 0;

            var prts = line.Split('+');
            var frst = prts[0].TrimEnd();
            var insideQuote = false;
            var insideParen = false;
            for (var linx = frst.Length - 1; linx >= 0; linx--)
            {
                var ltr = frst[linx];

                if (!insideParen)
                {
                    if (ltr == ')')
                    {
                        insideParen = true;
                        continue;
                    }
                }
                else
                {
                    if (ltr == '(')
                    {
                        insideParen = false;
                        continue;
                    }
                    else
                    {
                        continue;
                    }

                }

                if (!insideQuote)
                {
                    if (ltr == '\'')
                    {
                        insideQuote = true;
                        continue;
                    }
                }
                else
                {
                    var ltr2 = ' ';
                    if (linx < 0)
                    {
                        ltr2 = frst[linx - 1];
                    }

                    if ((ltr == '\'') && (ltr2 == '\''))
                    {
                        linx = linx - 1;
                        continue;
                    }


                    if ((ltr == '\''))
                    {
                        ret = linx;
                        break;
                    }
                    else { continue; }


                }

                //  if (ltr=='(')
                // {
                //       ret = linx +1;
                //     break;
                //  }

                if (!(char.IsLetterOrDigit(ltr) || ltr == '^' || ltr == '.' || ltr == '\'' || ltr == '@')) //||ltr=='('||ltr == ')'))
                {
                    ret = linx + 1;
                    break;
                }
            }



            return ret;
        }
        public static int getIndexOfEnd(string line)
        {
            var ret = line.Length;
            var lstPlsinx = line.LastIndexOf('+');
            var skipSpace = true;
            var insideQuote = false;
            var insideParen = false;
            for (var linx = lstPlsinx + 1; linx < line.Length; linx++)
            {
                if (linx == 45)
                {
                    int xc = 0;
                }

                ret = linx;
                var ltr = line[linx];

                if (!insideParen)
                {
                    if (ltr == '(')
                    {
                        insideParen = true;
                        continue;
                    }
                }
                else
                {
                    if (ltr == ')')
                    {
                        insideParen = false;
                        continue;
                    }
                    else
                    {
                        continue;
                    }

                }


                if (skipSpace == true)
                {
                    if (!char.IsWhiteSpace(ltr))
                    {
                        skipSpace = false;
                    }
                    else continue;
                }


                if (insideQuote)
                {
                    var ltr2 = ' ';
                    if (linx < line.Length)
                    {
                        ltr2 = line[linx + 1];
                    }

                    if ((ltr == '\'') && (ltr2 == '\''))
                    {
                        linx++;
                        ret = linx;
                        continue;
                    }

                    if (ltr == '\'')
                    {
                        ret = linx + 1;
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    if (ltr == '\'')
                    {
                        insideQuote = true;
                    }

                }

                if (ltr == ')')
                {
                    break;
                }

                if (!(char.IsLetterOrDigit(ltr) || ltr == '^' || ltr == '@' || ltr == '.' || ltr == '\'' || ltr == '%'))
                {
                    break;
                }
                ret = linx + 1;

            }
            return ret;
        }



    }

}
