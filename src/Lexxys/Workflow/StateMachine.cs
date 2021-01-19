// Lexxys Infrastructural library.
// file: StateMachine.cs
//
// Copyright (c) 2001-2014, Kraft Pro Utilities.
// You may use this code under the terms of the MIT license
//
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Xml;


namespace Lexxys.Workflow
{
	public static class StateMachineFactory
	{
		private static readonly char[] __trimmedChars = new char[] { '/', ' ', '\t' };
		public static StateMachine Create(string stateMachinePath)
		{
			if (stateMachinePath == null || stateMachinePath.Length == 0)
				throw new ArgumentNullException(nameof(stateMachinePath));
			string xpathQuery = stateMachinePath.Replace('\\', '/').Replace('.', '/').Trim(__trimmedChars);
			if (xpathQuery.Length == 0)
				throw EX.ArgumentOutOfRange(nameof(stateMachinePath), stateMachinePath);
			int i = xpathQuery.LastIndexOf('/');
			xpathQuery = i > 0 ?
				stateMachinePath.Substring(0, i) + "/stateMachines/stateMachine[@name=\"" + xpathQuery.Substring(i + 1) + "\"]":
				"//stateMachines/stateMachine[@name=\"" + xpathQuery + "\"]";
			StateMachine sm = Config.GetValue<StateMachine>(xpathQuery);
			return sm?.Clone();
		}

		public static StateMachine Create(string stateMachinePath, int initialState)
		{
			if (stateMachinePath == null || stateMachinePath.Length == 0)
				throw new ArgumentNullException(nameof(stateMachinePath));
			StateMachine sm = Create(stateMachinePath);
			if (sm != null)
				sm.Reset(initialState);
			return sm;
		}
	}

	/// <summary>
	/// 
	/// <remarks>
	/// stateMachines
	///		:stateMachine	name
	///		:*/state		name	id
	///		:*/transition	action	=> target
	/// 
	///		stateMachine	GettingReady
	///			=description	GettingReady Description
	///			=enum			GettingReadyCode
	///			=start			NotReady
	///			=end			Done
	///	
	/// 		state		NotReady	8
	/// 		state		Ready		10
	/// 		state		Done		254
	/// 		
	///			:state name			description
	///			:*/transition	action	target	description
	/// 
	///			state FirstReview	12
	///				transition	Loop	=> FirstReview		"Add Review Notes"
	///				transition	Forward	=> SecondReview		"Send to the Second Review"
	///					=guard	IsFirstReviewApproved
	///					
	///			state SecondReview	22
	///				transition	Back	=> FirstReview		"Back to the First Review"
	///				transition	Loop	=> SecondReview		"Add Review Notes"
	///				transition	Forward	=> finalCorrection	"Send to the Final Corrections"
	///					=guard	IsSecondReviewApproved
	///					
	///			state FinalCorrections	32
	///				transition	BackBack=> FirstReview		"Back to the First Review"
	///					=guard	IsCompletelyChanged
	///				transition	Back	=> SecondReview		"Back to the Second Review"
	///				transition	Loop
	///				transition	Forward
	///					=guard	IsReadyForClient
	///					
	///			state SentToClient
	///			state SignedAndReceived
	///			state SpecialHandling
	///			state Done
	/// </remarks>
	/// </summary>
	[DebuggerDisplay("Name = {Name}, State = {CurrentState.Name}, {_states.Length} states, {_transitions.Length} transitions")]
	public sealed class StateMachine
	{
		private readonly string _name;
		private readonly Type _enumType;
		private readonly StateMachineState[] _states;
		private readonly StateMachineTransition[] _transitions;
		private StateMachineState _currentState;

		private StateMachine(string name, Type enumType, StateMachineState[] states, StateMachineTransition[] transitions)
		{
			if (states == null || states.Length <= 0)
				throw new ArgumentNullException(nameof(states));
			_name = name ?? throw new ArgumentNullException(nameof(name));
			_enumType = enumType;
			_states = states;
			_transitions = transitions ?? throw new ArgumentNullException(nameof(transitions));
			_currentState = states[0];
		}

		public string Name
		{
			get { return _name; }
		}

		public StateMachineState CurrentState
		{
			get { return _currentState; }
		}

		public Type EnumType
		{
			get { return _enumType; }
		}

		public bool IsFinal
		{
			get { return _currentState.IsFinal; }
		}

		public void ApplyAction(string actionName)
		{
			StateMachineTransition transition = _currentState.Transitions.First(x => x.Action == actionName);
			if (transition == null)
				throw new ArgumentOutOfRangeException(nameof(actionName), actionName, null).Add("currentState", _currentState);
			_currentState = transition.Target;
		}

		public void Reset()
		{
			_currentState = _states[0];
		}

		public void Reset(StateMachineState state)
		{
			if (!_states.Contains(state))
				throw new ArgumentOutOfRangeException(nameof(state), state, null);
			_currentState = state;
		}

		public void Reset(int stateId)
		{
			_currentState = GetState(stateId) ?? throw EX.ArgumentOutOfRange(nameof(stateId), stateId);
		}

		public ReadOnlyCollection<StateMachineState> GetStates(int distance)
		{
			if (distance < -1)
				throw new ArgumentOutOfRangeException(nameof(distance), distance, null);
			if (distance == -1)
			{
				return new ReadOnlyCollection<StateMachineState>(_states);
			}
			if (distance >= _states.Length)
				distance = _states.Length - 1;
			if (distance == 0)
			{
				var st1 = new StateMachineState[1];
				st1[0] = _currentState;
				return new ReadOnlyCollection<StateMachineState>(st1);
			}

			var result = new HashSet<StateMachineState> { _currentState };
			foreach (var item in _currentState.Transitions)
			{
				result.Add(item.Target);
			}
			while (--distance > 0 && result.Count < _states.Length)
			{
				var addendum = new HashSet<StateMachineState>();
				foreach (var state in result)
				{
					foreach (var item in state.Transitions)
					{
						if (!result.Contains(item.Target))
							addendum.Add(item.Target);
					}
				}
				if (addendum.Count == 0)
					break;
				result.UnionWith(addendum);
			}
			if (result.Count == _states.Length)
				return new ReadOnlyCollection<StateMachineState>(_states);
			var result2 = new StateMachineState[result.Count];
			result.CopyTo(result2);
			return new ReadOnlyCollection<StateMachineState>(result2);
		}

		public ReadOnlyCollection<StateMachineTransition> GetTransitions(int distance)
		{
			if (distance < -1)
				throw new ArgumentOutOfRangeException(nameof(distance), distance, null);
			if (distance == -1)
				return new ReadOnlyCollection<StateMachineTransition>(_transitions);
			if (distance >= _states.Length)
				distance = _states.Length - 1;
			if (distance == 0)
				return _currentState.Transitions;
			var result = new HashSet<StateMachineTransition>(_currentState.Transitions);
			while (--distance >= 0 && result.Count < _transitions.Length)
			{
				var addendum = new HashSet<StateMachineTransition>();
				foreach (var trans in result)
				{
					foreach (var item in trans.Target.Transitions)
					{
						if (!result.Contains(item))
							addendum.Add(item);
					}
				}
				if (addendum.Count == 0)
					break;
				result.UnionWith(addendum);
			}
			if (result.Count == _states.Length)
				return new ReadOnlyCollection<StateMachineTransition>(_transitions);
			var result2 = new StateMachineTransition[result.Count];
			result.CopyTo(result2);
			return new ReadOnlyCollection<StateMachineTransition>(result2);
		}

		public StateMachineState GetState(int stateId)
		{
			int i = Array.FindIndex(_states, x => x.Id == stateId);
			return i < 0 ? null: _states[i];
		}

		public StateMachineState FindStateByName(string stateName)
		{
			int i = Array.FindIndex(_states, x => x.Name == stateName);
			return i < 0 ? null: _states[i];
		}

		public ReadOnlyCollection<StateMachineTransition> FindTransition(string actionName)
		{
			var result = new List<StateMachineTransition>(_transitions.Length);
			foreach (var item in _transitions)
			{
				if (item.Action == actionName)
					result.Add(item);
			}
			return result.AsReadOnly();
		}

		#region StateMachine Configuration suppurt

		private const int NotId = Int32.MinValue;

		class StateRecord
		{
			public int Id;
			public string Name;
			public Dictionary<string, string> Attribs;
			public TransitionRecord[] Transition;

			public static StateRecord FromXml(XmlReader reader)
			{
				var rec = new StateRecord() { Name=reader["name"] };
				if (rec.Name == null)
				{
					reader.Skip();
					return null;
				}
				rec.Id = NotId;
				string s = reader["id"];
				if (s != null && !Int32.TryParse(s, out rec.Id))
				{
					reader.Skip();
					return null;
				}
				rec.Attribs = new Dictionary<string, string>();
				reader.MoveToFirstAttribute();
				do
				{
					rec.Attribs.Add(reader.Name, reader.Value);
				} while (reader.MoveToNextAttribute());
				reader.MoveToElement();
				var tran = new List<TransitionRecord>();
				if (!reader.IsEmptyElement && reader.Read())
				{
					while (reader.MoveToContent() == XmlNodeType.Element && reader.Name == "transition")
					{
						var x = TransitionRecord.FromXml(reader);
						if (x != null)
							tran.Add(x);
					}
					while (reader.NodeType != XmlNodeType.EndElement && reader.Read())
						;
				}
				rec.Transition = (tran.Count == 0) ? EmptyArray<TransitionRecord>.Value: tran.ToArray();
				reader.Skip();
				return rec;
			}
		}

		class TransitionRecord
		{
			public int TargetId;
			public string TargetName;
			public string Action;
			public string Procedure;
			public string Condition;
			public Dictionary<string, string> Attribs;

			public static TransitionRecord FromXml(XmlReader reader)
			{
				var rec = new TransitionRecord() { Action = reader["action"], TargetName = reader["target"] };
				if (rec.TargetName == null)
				{
					reader.Skip();
					return null;
				}
				if (!Int32.TryParse(rec.TargetName, out rec.TargetId))
					rec.TargetId = NotId;
				rec.Condition = reader["condition"];
				rec.Procedure = reader["procedure"];
				rec.Attribs = new Dictionary<string, string>();
				reader.MoveToFirstAttribute();
				do
				{
					rec.Attribs.Add(reader.Name, reader.Value);
				} while (reader.MoveToNextAttribute());
				reader.MoveToContent();
				reader.Skip();
				return rec;
			}
		}

		public static StateMachine FromXml(XmlReader reader)
		{
			if (reader.MoveToContent() != XmlNodeType.Element)
				reader.Read();
			string name = reader["name"] ?? reader.Name;
			Type enumType = Factory.GetType(reader["enum"]);

			var rr = new List<StateRecord>();
			reader.Read();
			while (reader.MoveToContent() == XmlNodeType.Element && reader.Name == "state")
			{
				var r = StateRecord.FromXml(reader);
				if (r != null)
					rr.Add(r);
			}

			if (enumType != null)
			{
				for (int i = 0; i < rr.Count; ++i)
				{
					if (rr[i].Id == NotId)
					{
						rr[i].Id = (int)Enum.Parse(enumType, rr[i].Name, true);
					}
				}
			}


			var allStates = new StateMachineState[rr.Count];
			var statesLookup = new Dictionary<string, StateMachineState>(rr.Count);
			for (int i = 0; i < allStates.Length; ++i)
			{
				allStates[i] = new StateMachineState(rr[i].Id == NotId ? 0: rr[i].Id, rr[i].Name, rr[i].Attribs);
				statesLookup.Add(allStates[i].Name.ToUpperInvariant(), allStates[i]);
			}

			for (int i = 0; i < allStates.Length; ++i)
			{
				StateMachineTransition[] tts = Array.ConvertAll(rr[i].Transition,
					x => new StateMachineTransition(x.Action, x.Condition, x.Procedure, allStates[i], statesLookup[x.TargetName.ToUpperInvariant()], x.Attribs));
				allStates[i].SetTransitions(tts);
			}

			var allTransitions = new List<StateMachineTransition>(rr.Count * rr.Count);
			foreach (StateMachineState state in allStates)
			{
				allTransitions.AddRange(state.Transitions);
			}

			return new StateMachine(name, enumType, allStates, allTransitions.ToArray());
		}
		#endregion

		internal StateMachine Clone()
		{
			return new StateMachine(_name, _enumType, _states, _transitions);
		}
	}

	[DebuggerDisplay("Id = {Id}, Name = {Name}, {_transitions.Length} transitions")]
	public sealed class StateMachineState
	{
		private readonly int _id;
		private readonly string _name;
		private readonly Dictionary<string, string> _attribs;
		private StateMachineTransition[] _transitions;

		internal StateMachineState(int id, string name, Dictionary<string, string> attribs)
		{
			_id = id;
			_name = name;
			_attribs = attribs;
			_attribs["id"] = id.ToString(CultureInfo.InvariantCulture);
			_attribs["name"] = name;
		}

		internal void SetTransitions(StateMachineTransition[] transitions)
		{
			_transitions = transitions;
		}

		public int Id
		{
			get { return _id; }
		}

		public string Name
		{
			get { return _name; }
		}

		public string this[string attrib]
		{
			get
			{
				if (!_attribs.TryGetValue(attrib, out string result))
					result = "";
				return result;
			}
		}

		public bool IsFinal
		{
			get { return _transitions.All(item => item.Target.Id == _id); }
		}

		public ReadOnlyCollection<StateMachineTransition> Transitions
		{
			get { return new ReadOnlyCollection<StateMachineTransition>(_transitions); }
		}
	}

	[DebuggerDisplay("Id = {Id}, Command = {Action}, Guard = {Condition}, Source = {Source.Name}, Target = {Target.Name}")]
	public class StateMachineTransition
	{
		private readonly string _id;
		private readonly string _name;
		private readonly string _guard;
		private readonly string _procedure;
		private readonly Dictionary<string, string> _attribs;
		private readonly StateMachineState _sourceState;
		private readonly StateMachineState _targetState;

		public delegate bool TransitionGuard(string condition);

		public StateMachineTransition(string name, string guard, string procedure, StateMachineState sourceState, StateMachineState targetState, Dictionary<string, string> attributes)
		{
			_name = name;
			_guard = guard;
			_procedure = procedure;
			_sourceState = sourceState;
			_targetState = targetState;
			_attribs = attributes;
			if (!_attribs.TryGetValue("id", out _id))
			{
				_id = sourceState.Id.ToString(CultureInfo.InvariantCulture) + "." + targetState.Id.ToString(CultureInfo.InvariantCulture);
				_attribs["id"] = _id;
			}
			_attribs["name"] = name;
			_attribs["source"] = sourceState.Name;
			_attribs["target"] = targetState.Name;
			_attribs["condition"] = guard;
		}

		public string Id
		{
			get { return _id; }
		}

		public string Action
		{
			get { return _name; }
		}

		public string Condition
		{
			get { return _guard; }
		}

		public string Procedure
		{
			get { return _procedure; }
		}

		public StateMachineState Source
		{
			get { return _sourceState; }
		}

		public StateMachineState Target
		{
			get { return _targetState; }
		}

		public string this[string attrib]
		{
			get
			{
				if (!_attribs.TryGetValue(attrib, out string result))
					result = "";
				return result;
			}
		}
	}
}
