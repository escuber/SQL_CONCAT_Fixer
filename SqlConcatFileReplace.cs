using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace SQL_CONCAT_Fixer
{
    public class SqlConcatFileReplace
    {
        public int cntr { get; set; }
        public int totalLines{ get; set; }
        public int totalUpdates { get; set; }
        public string[] _inputLines { get; set; }
        public string[] _OutputLines{ get; set; }

        public string _filename { get; set; }
        public string _input_path { get; set; }
        public FileInfo fi { get; set; }
        public string updatePath = @"C:\devlab\teststudio\UpdatedSql\";
        public SqlConcatFileReplace(FileInfo file)
        {
            fi = file;
            _input_path = file.DirectoryName;
            _filename = file.Name;
            cntr = 0;
//            cclog.wlf("doing: "+file.FullName);

        }
        public SqlConcatFileReplace()
        {
         
        }

        public void processFile()
        {
           // cclog.wlf("processing: " + fi.FullName);

            _inputLines = File.ReadAllLines(_input_path+'\\'+_filename);
            _inputLines = removeComments(_inputLines);
            totalLines = _inputLines.Length;
            totalUpdates = 0;
            _OutputLines = _inputLines;

            var chgLines = 0;
            for (var inx = 0; inx < _inputLines.Length; inx++)
            {
                totalLines++;
                bool noprocess = false;

                if (!_inputLines[inx].Trim().StartsWith("@"))
                     _inputLines = addWithNext(inx, _inputLines);
                // the following method correct any '+' that start on a new line by putting the lines together
                var bfr= _inputLines[inx];
    
               
                var replaceLine = bfr;

                if ((_inputLines[inx].IndexOf("+") < 0)
                || (_inputLines[inx].IndexOf("--") > 0)
                || (_inputLines[inx].IndexOf("DECLARE") > 0)
                || (_inputLines[inx].IndexOf("CASE") > 0)
                || (_inputLines[inx].IndexOf("WHEN") > 0)
                || (_inputLines[inx].IndexOf("+=") > 0)

                //if (_inputLines[inx].IndexOf("ELSE") > 0) continue;
                || (_inputLines[inx].IndexOf("END") > 0)
                ||((_inputLines[inx].IndexOf("SET") > 0 && _inputLines[inx].ToUpper().IndexOf("REPLACE") > 0))
                )
                {
                    replaceLine = bfr;
                }
                else {
                     replaceLine = preprocess(_inputLines[inx], inx);
                }


                if (replaceLine != bfr)
                {
                    totalUpdates++;
 
                }
                _OutputLines[inx] = replaceLine;


            }

            var linescnt = 0;// _OutputLines.Length;
            while (_OutputLines.Length != linescnt)
            {
                linescnt = _OutputLines.Length;
                _OutputLines = removeExtraLines(_OutputLines);

            }
            

            //
            //  Free the memory
            //
            if(this.totalUpdates>0)
                WriteAlterFile();
            _inputLines = null;
            _OutputLines = null;
        }
        private string[] removeExtraLines(string[] lines)
        {
            var o = new List<string>();
            for (int x = 0; x < lines.Count() - 1; x++)
            {
                if (string.IsNullOrEmpty(lines[x].Trim()) && string.IsNullOrEmpty(lines[x + 1].Trim()))
                {
                   // Console.WriteLine("Skipping Line:"+ lines[x]);
                }
                else {
                    o.Add(lines[x]);
                }
            }
            o.Add(lines[lines.Count()-1]);
            return o.ToArray();
        
        }
        private string[] removeComments(string[] lines)
        {
            for (var inx = 0; inx < lines.Length; inx++)
            {
                var line = lines[inx];
                if (line.IndexOf("--") >= 0) {
                    lines[inx] = line.Substring(0, line.IndexOf("--"));
                }

            }
            return lines;



        }
        private string preprocess( string line,int inx)
        {
            var tline = line.Trim();
            if (tline.StartsWith("+")) return line;

            this.fnList = new List<string>();
            this.funcDict = new Dictionary<string, string>();

            var rslts = removeFunctions(line,inx);          
            rslts = DoParmsInParmsAndProcess(rslts,inx);
            rslts = returnFunctions(rslts);
            return rslts;
        }
        private string[] addWithNext(int inx,string[] lines)
        {


            var addr = 1;
            while (inx + addr  < lines.Count() - 1)
            {

                var tline = lines[inx + addr].Trim();
                if (lines[inx].TrimEnd().EndsWith(","))
                {
                    while (lines[inx + addr].Trim().Length == 0) addr++;
                    lines[inx] = lines[inx] + lines[inx + addr].Trim();
                    lines[inx + addr] = "";
                    addr++;
                    continue;
                }
                else
                    break;

            }
            addr = 1;
                while (inx < lines.Count() - 1)
                {
                var tline = lines[inx + addr].Trim();
                tline = lines[inx + addr].Trim();
                if (tline.StartsWith("+"))
                {
                    lines[inx] = lines[inx ] + tline;
                    lines[inx + addr] = "";
                    addr++;


                }
                else
                {
                    break;
                }
            
            }
            return lines;
        
        
        }
        private void BackupOriginal()
        { 

            var backFile = _input_path + _filename.Replace(".sql", ".bak");
            if (File.Exists(backFile)) File.Delete(backFile);
            if (File.Exists(updatePath+_filename)) File.Delete(updatePath + _filename);

            // these two lines make the backup and replace the original
             File.Move(_input_path + _filename, backFile);
            // 
        }

        public void ReplaceOriginal()
        {
            BackupOriginal();
            File.WriteAllLines(_input_path + _filename, _OutputLines);    
        }
        public void WriteAlterFile()
        {
            if (File.Exists(updatePath+_filename)) File.Delete(updatePath + _filename);
            // the one below makes the alter copies for loading the server
            _OutputLines[0] = _OutputLines[0].Replace("CREATE ", "ALTER ");
            File.WriteAllLines(updatePath + _filename, _OutputLines);

        }

        public static void testALine()
        {
            var myo= new SqlConcatFileReplace();


            var myline = @"  SET @LabComment = CONVERT(VARCHAR, GETDATE(), 101) + ' ' + RIGHT(CONVERT(VARCHAR, GETDATE()), 7)+ ' SoapRpt' + ' LabCorp Web COC Order Registration Number: '+ CONVERT(VARCHAR(100), @LabOrderId) ";


            var rslts = myo.removeFunctions(myline,0);
            cclog.wlf(rslts);
            rslts =myo.DoParmsInParmsAndProcess(rslts, 0);
            rslts = myo.returnFunctions(rslts);
            cclog.wlf(rslts);
            var x = 4;
        }
   
        private int findEndofPar(string line,int inx)
        {
            if (line[0] != '(') return -1;
            if (line.IndexOf('(', 1) < 0) return line.IndexOf(')');

            var linesofar = "";

            if (line.IndexOf('(', 1) < line.IndexOf(')'))
            {
                // in here what we must do is
                // add everythign up to the next '('
                // then call this function to find the back side

                var newsub = line.Substring(0, line.IndexOf('(', 1));
                var mylen = line.Length - newsub.Length;
                //line.Substring(line.IndexOf('(', 1)).Length;

                var remainder = line.Substring(newsub.Length);
                var endOfPar = findEndofPar(remainder,inx);
                var toremove = remainder.Length- endOfPar+1;
                var restofLine = remainder.Substring(endOfPar + 1);
                return line.IndexOf(')',line.Length- restofLine.Length);
                //linesofar =
            }
            else
                return line.IndexOf(')');

            
        }
        private Dictionary<string, string> funcDict = new Dictionary<string, string>();
        private int fnNumber = 0;
        private List<string> fnList = new List<string>();
        private string removeFunctions(string line,int inx)
        {
            this.funcDict = new Dictionary<string, string>();
            var bfrLine = "";
            var CurrLine = line;
            fnNumber = 0;
            fnList = new List<string>();
            while (bfrLine != CurrLine)
            {
                bfrLine = CurrLine;
                CurrLine = removeNextFunction(bfrLine,inx);
            }

            return bfrLine;
        
        }

        private string removeNextFunction(string line,int inx)
        {
            if (line.IndexOf("+") < 0) return line;
            if (line.ToUpper().IndexOf("DECLARE") >= 0) return line;
            if (line.IndexOf("--") >= 0) return line;
            if (line.ToUpper().IndexOf("CASE") >= 0) return line;


            var openParns =line.ToArray().Count(i => i == '(');
            var closeParns= line.ToArray().Count(i => i == ')');
            if (openParns == 0) return line;
            if (openParns != closeParns) return line;


            var strtParn = line.IndexOf('(', 1);
            // seems reasonable lets remove the functions
            var newsub = line.Substring(0, line.IndexOf('(', 1));
            var mylen = line.Length - newsub.Length;


            var strtPos =  newsub.Length;
            var remainder = line.Substring(newsub.Length);
            var endOfPar = findEndofPar(remainder,inx) + 1;/// +newsub.Length;
            var toremove = remainder.Length - strtPos + endOfPar + 1;
            var endPos = strtPos+endOfPar;                
            var restofLine = remainder.Substring(0,endOfPar );
            var thisFunction = line.Substring(strtPos, endOfPar);
            var fnName = "";
            var x = strtPos-1;
            
            while (char.IsLetter(line[x])&&(!char.IsWhiteSpace(line[x])))
            { x--;
                endOfPar++;
            }
            strtPos = x;
            thisFunction = line.Substring(strtPos+1, endOfPar);
            this.fnList.Add(thisFunction);

            var bf = line.Substring(0, strtPos);
            var af = line.Substring(strtPos + endOfPar+1);
            var sym = $"xoxo{fnList.Count()}xoxo";
            this.funcDict[sym] = thisFunction;
            line =line.Substring(0, strtPos) + $" xoxo{fnList.Count()}xoxo " + af;

            //cclog.wlf(line);
            return line;

        
        }
        private string returnFunctions(string line)
        {
            for (int x =0; x<fnList.Count; x++)
            {
                var sym = $"xoxo{x+1}xoxo";
                var fn = funcDict[sym];
                line = line.Replace(sym, fn);
            }

            return line;


        }

        private  string DoParmsInParmsAndProcess(string line, int inx)
        {
            var ret = "";
          
         
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





        private string spllitSpecials_and_processLine(string line, int lineinx)
        {
            var ret = "";
          //  cclog.wlf("rslt:" + line);

           Dictionary<string,string> qph = new Dictionary<string, string>();

            var noquoteLine = "";

            //var frst = prts[0].TrimEnd();
            var insideQuote = false;
            var insideParen = false;
            var phraseInx = 0;
            var currentQuote = "";
            var firstQuote = false;
            for (var linx = 0;linx< line.Length; linx++)
            {
                var ltr = line[linx];

                if (!insideQuote)
                {
                    if (ltr == '\'')
                    {
                        // this is the start of a quote
                        insideQuote = true;
                        currentQuote =""+ ltr;
                        noquoteLine += $" ^{phraseInx}^ ";
                    }
                    else {
                        noquoteLine += ltr;
                    
                    }
                }
                else
                {
                    // we are inside a quote so we need to ignore two in a row
                    var ltr2 = ' ';
                    if (linx < line.Length-1)
                    {
                        ltr2 = line[linx+1];
                    }
                    if ((ltr == '\'') && (ltr2 == '\'') && !firstQuote)
                    {
                        // this is the second quote of a pair in a line
                        firstQuote = true;
                        currentQuote += ltr;
                        // linx = linx - 1;
                        continue;
                    }

                    if (ltr == '\'' && firstQuote )
                    {
                        // this is the second quote of a pair in a line
                        firstQuote = false;
                        currentQuote += ltr;
                        // linx = linx - 1;
                        continue;
                    }
                    else {
                        if (ltr == '\'')
                        {
                            insideQuote = false;
                            // this is the end of the quote 
                            // put the quote in the collection 
                            currentQuote += ltr;
                            qph[$"^{phraseInx++}^"] = currentQuote;

                        }
                        else {
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


        private string processLine(string line, int inx)
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
                ret = $"{startPart} {makeConcat(midPart, inx)} {endPart}";

            }
            return ret;
        }
        private string makeConcat(string line, int x)
        {
            var ret = line;

            var prts = line.Split(new[] { '+' },StringSplitOptions.RemoveEmptyEntries);
            // test for any blank spaces
            if (prts.Length == 1) return line;
            for (var inx =0;inx<prts.Length;inx++)
            {
                prts[inx]=prts[inx].Trim();
            }

            var comaLine = string.Join(",", prts);
            ret = $"concat({comaLine})";


            return ret;

        }

        private int getIndexOfStart(string line)
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

                
                if (!(char.IsLetterOrDigit(ltr)|| ltr=='^' || ltr == '.' || ltr == '\'' || ltr == '@')) 
                {
                    ret = linx + 1;
                    break;
                }
            }



            return ret;
        }
        private int getIndexOfEnd(string line)
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
