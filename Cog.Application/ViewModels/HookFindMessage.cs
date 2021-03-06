﻿using System.Windows.Input;
using GalaSoft.MvvmLight.Messaging;

namespace SIL.Cog.Application.ViewModels
{
	public class HookFindMessage : MessageBase
	{
		private readonly ICommand _findCommand;

		public HookFindMessage(ICommand findCommand)
		{
			_findCommand = findCommand;
		}

		public ICommand FindCommand
		{
			get { return _findCommand; }
		}
	}
}
