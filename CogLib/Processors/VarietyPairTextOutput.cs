using System.IO;
using System.Linq;
using System.Text;

namespace SIL.Cog.Processors
{
	public class VarietyPairTextOutput : ProcessorBase<VarietyPair>
	{
		private readonly string _fileName;
		private readonly string _alignerID;

		public VarietyPairTextOutput(CogProject project, string fileName, string alignerID)
			: base(project)
		{
			_fileName = fileName;
			_alignerID = alignerID;
		}

		public string FileName
		{
			get { return _fileName; }
		}

		public string AlignerID
		{
			get { return _alignerID; }
		}

		public override void Process(VarietyPair varietyPair)
		{
			using (var writer = new StreamWriter(Path.Combine(_fileName, string.Format("{0}+{1}.txt", varietyPair.Variety1, varietyPair.Variety2))))
			{
				writer.WriteLine("Lexical Similarity: {0:0.0####}", varietyPair.LexicalSimilarityScore);
				writer.WriteLine("Phonetic Similarity: {0:0.0####}", varietyPair.PhoneticSimilarityScore);
				writer.WriteLine();

				var sb = new StringBuilder();
				foreach (SoundChange change in varietyPair.SoundChanges)
				{
					sb.AppendLine(change.ToString());
					sb.AppendLine("Segment\tProb");
					foreach (var correspondence in change.ObservedCorrespondences.Select(corr => new {Phone = corr, Probability = change[corr]}).OrderByDescending(corr => corr.Probability))
					{
						sb.AppendFormat("{0}\t{1:0.0####}", correspondence.Phone, correspondence.Probability);
						sb.AppendLine();
					}
					sb.AppendLine();
				}
				writer.WriteLine(sb);

				IAligner aligner = Project.Aligners[_alignerID];
				foreach (WordPair pair in varietyPair.WordPairs.OrderByDescending(wp => wp.PhoneticSimilarityScore))
				{
					IAlignerResult results = aligner.Compute(pair);
					Alignment alignment = results.GetAlignments().First();
					writer.WriteLine(pair.Word1.Sense.Gloss);
					if (!string.IsNullOrEmpty(pair.Word1.Sense.Category))
						writer.WriteLine(pair.Word1.Sense.Category);
					writer.Write(alignment.ToString(pair.AlignmentNotes));
					writer.WriteLine("Score: {0:0.0####}", pair.PhoneticSimilarityScore);
					if (pair.AreCognatesPredicted)
						writer.WriteLine("***Cognate***");
					writer.WriteLine();
				}
			}
		}
	}
}