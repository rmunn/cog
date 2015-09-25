﻿using CommandLine;

namespace SIL.Cog.CommandLine
{
	class Program
	{
		static int Main(string[] args)
		{
			ReturnCodes retcode = Parser.Default.ParseArguments<SyllabifyVerb, PairsVerb, AlignmentVerb, CognatesVerb, ClusterVerb>(args)
				.Return((VerbBase opts) => opts.RunAsPipe(), (errs) => ReturnCodes.UnknownVerb);
			return (int)retcode;
		}
	}

}
