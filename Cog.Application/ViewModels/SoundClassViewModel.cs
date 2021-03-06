using SIL.Cog.Domain;

namespace SIL.Cog.Application.ViewModels
{
	public class SoundClassViewModel : ChangeTrackingViewModelBase
	{
		private readonly SoundClass _soundClass;
		private int _sonority;

		public SoundClassViewModel(SoundClass soundClass)
			: this(soundClass, -1)
		{
		}

		public SoundClassViewModel(SoundClass soundClass, int sonority)
		{
			_soundClass = soundClass;
			_sonority = sonority;
		}

		public string Name
		{
			get { return _soundClass.Name; }
		}

		public int Sonority
		{
			get { return _sonority; }
			set { SetChanged(() => Sonority, ref _sonority, value); }
		}

		public string Type
		{
			get
			{
				var nc = _soundClass as NaturalClass;
				if (nc != null)
					return nc.Type == CogFeatureSystem.ConsonantType ? "Consonant" : "Vowel";
				var unc = _soundClass as UnnaturalClass;
				if (unc != null)
					return "Segment";
				return "";
			}
		}

		public string Description
		{
			get
			{
				var nc = _soundClass as NaturalClass;
				if (nc != null)
					return nc.FeatureStruct.GetString();
				var unc = _soundClass as UnnaturalClass;
				if (unc != null)
					return string.Join(", ", unc.Segments);
				return "";
			}
		}

		internal SoundClass DomainSoundClass
		{
			get { return _soundClass; }
		}
	}
}
