using System;

namespace SIL.Cog.Application.Services
{
	public interface IBusyService
	{
		void ShowBusyIndicatorUntilFinishDrawing();
		void ShowBusyIndicator(Action action);
	}
}
