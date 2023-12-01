# SQL_CONCAT_Fixer

Changes all use of '||' in stored procs to the concat function.   It was more of a challange than I originally thought, but I got it done in less time than updating and testing all the several thousand line stored procs that I was assigned to change.

Doing this is totally unnecessary with the 2019 release of SQL server but before that it was a big deal.  I didn't trust my eyes and hands doing all the updates so I wrote this and it did a much better job.  

