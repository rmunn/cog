using GalaSoft.MvvmLight;
using SIL.Cog.Domain;
using SIL.Machine.NgramModeling;

namespace SIL.Cog.Application.ViewModels
{
	public class SoundChangeViewModel : ViewModelBase
	{
		private readonly SoundContext _domainLhs;
		private readonly Ngram<Segment> _correspondence;
		private readonly SoundChangeLhsViewModel _lhs;
		private readonly double _prob;
		private readonly int _frequency;

		public SoundChangeViewModel(SoundContext lhs, Ngram<Segment> correspondence, double probability, int frequency)
		{
			_domainLhs = lhs;
			_correspondence = correspondence;
			_lhs = new SoundChangeLhsViewModel(lhs);
			_prob = probability;
			_frequency = frequency;
		}

		public SoundChangeLhsViewModel Lhs
		{
			get { return _lhs; }
		}

		public string Correspondence
		{
			get { return _correspondence.ToString(); }
		}

		public double Probability
		{
			get { return _prob; }
		}

		public int Frequency
		{
			get { return _frequency; }
		}

		internal SoundContext DomainSoundChangeLhs
		{
			get { return _domainLhs; }
		}

		internal Ngram<Segment> DomainCorrespondence
		{
			get { return _correspondence; }
		}
	}
}
