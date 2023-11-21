#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Strategies.Algo
File: StrategyHelper.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Strategies
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Algo.Strategies.Protective;
	using StockSharp.Algo.Strategies.Quoting;
	using StockSharp.Algo.Derivatives;

	/// <summary>
	/// Extension class for <see cref="Strategy"/>.
	/// </summary>
	public static partial class StrategyHelper
	{
		/// <summary>
		/// To create initialized object of buy order at market price.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Strategy.Volume"/> value is used.</param>
		/// <returns>The initialized order object.</returns>
		/// <remarks>
		/// The order is not registered, only the object is created.
		/// </remarks>
		public static Order BuyAtMarket(this Strategy strategy, decimal? volume = null)
		{
			return strategy.CreateOrder(Sides.Buy, null, volume);
		}

		/// <summary>
		/// To create the initialized order object of sell order at market price.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Strategy.Volume"/> value is used.</param>
		/// <returns>The initialized order object.</returns>
		/// <remarks>
		/// The order is not registered, only the object is created.
		/// </remarks>
		public static Order SellAtMarket(this Strategy strategy, decimal? volume = null)
		{
			return strategy.CreateOrder(Sides.Sell, null, volume);
		}

		/// <summary>
		/// To create the initialized order object for buy.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="price">Price.</param>
		/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Strategy.Volume"/> value is used.</param>
		/// <returns>The initialized order object.</returns>
		/// <remarks>
		/// The order is not registered, only the object is created.
		/// </remarks>
		public static Order BuyAtLimit(this Strategy strategy, decimal price, decimal? volume = null)
		{
			return strategy.CreateOrder(Sides.Buy, price, volume);
		}

		/// <summary>
		/// To create the initialized order object for sell.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="price">Price.</param>
		/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Strategy.Volume"/> value is used.</param>
		/// <returns>The initialized order object.</returns>
		/// <remarks>
		/// The order is not registered, only the object is created.
		/// </remarks>
		public static Order SellAtLimit(this Strategy strategy, decimal price, decimal? volume = null)
		{
			return strategy.CreateOrder(Sides.Sell, price, volume);
		}

		/// <summary>
		/// To create the initialized order object.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="side">Order side.</param>
		/// <param name="price">The price. If <see langword="null" /> value is passed, the order is registered at market price.</param>
		/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Strategy.Volume"/> value is used.</param>
		/// <returns>The initialized order object.</returns>
		/// <remarks>
		/// The order is not registered, only the object is created.
		/// </remarks>
		public static Order CreateOrder(this Strategy strategy, Sides side, decimal? price, decimal? volume = null)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			var security = strategy.Security;

			if (security == null)
				throw new InvalidOperationException(LocalizedStrings.SecurityNotSpecified);

			var order = new Order
			{
				Portfolio = strategy.Portfolio,
				Security = strategy.Security,
				Side = side,
				Volume = volume ?? strategy.Volume,
			};

			if (price == null)
			{
				//if (security.Board.IsSupportMarketOrders)
				order.Type = OrderTypes.Market;
				//else
				//	order.Price = strategy.GetMarketPrice(direction) ?? 0;
			}
			else
				order.Price = price.Value;

			return order;
		}

		/// <summary>
		/// To close open position by market (to register the order of the type <see cref="OrderTypes.Market"/>).
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="slippage">The slippage level, admissible at the order registration. It is used, if the order is registered using the limit order.</param>
		/// <remarks>
		/// The market order is not operable on all exchanges.
		/// </remarks>
		public static void ClosePosition(this Strategy strategy, decimal slippage = 0)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			var position = strategy.Position;

			if (position != 0)
			{
				var volume = position.Abs();

				var order = position > 0 ? strategy.SellAtMarket(volume) : strategy.BuyAtMarket(volume);

				if (order.Type != OrderTypes.Market)
				{
					order.Price += (order.Side == Sides.Buy ? slippage : -slippage);
				}

				strategy.RegisterOrder(order);
			}
		}

		private const string _isEmulationModeKey = "IsEmulationMode";

		/// <summary>
		/// To get the strategy start-up mode (paper trading or real).
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <returns>If the paper trading mode is used - <see langword="true" />, otherwise - <see langword="false" />.</returns>
		public static bool GetIsEmulation(this Strategy strategy)
		{
			return strategy.Environment.GetValue(_isEmulationModeKey, false);
		}

		/// <summary>
		/// To set the strategy start-up mode (paper trading or real).
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="isEmulation">If the paper trading mode is used - <see langword="true" />, otherwise - <see langword="false" />.</param>
		public static void SetIsEmulation(this Strategy strategy, bool isEmulation)
		{
			strategy.Environment.SetValue(_isEmulationModeKey, isEmulation);
		}

		private const string _optionDeskKey = "OptionDesk";

		/// <summary>
		/// To get the <see cref="IOptionDesk"/>.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <returns><see cref="IOptionDesk"/>.</returns>
		public static IOptionDesk GetOptionDesk(this Strategy strategy)
		{
			return strategy.Environment.GetValue<IOptionDesk>(_optionDeskKey);
		}

		/// <summary>
		/// To set the <see cref="IOptionDesk"/>.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="desk"><see cref="IOptionDesk"/>.</param>
		public static void SetIsEmulation(this Strategy strategy, IOptionDesk desk)
		{
			strategy.Environment.SetValue(_optionDeskKey, desk);
		}

		/// <summary>
		/// To get market data value for the strategy instrument.
		/// </summary>
		/// <typeparam name="T">The type of the market data field value.</typeparam>
		/// <param name="strategy">Strategy.</param>
		/// <param name="field">Market-data field.</param>
		/// <returns>The field value. If no data, the <see langword="null" /> will be returned.</returns>
		public static T GetSecurityValue<T>(this Strategy strategy, Level1Fields field)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			return strategy.GetSecurityValue<T>(strategy.Security, field);
		}

		///// <summary>
		///// To get market price for the instrument by maximal and minimal possible prices.
		///// </summary>
		///// <param name="strategy">Strategy.</param>
		///// <param name="side">Order side.</param>
		///// <returns>The market price. If there is no information on maximal and minimal possible prices, then <see langword="null" /> will be returned.</returns>
		//public static decimal? GetMarketPrice(this Strategy strategy, Sides side)
		//{
		//	if (strategy == null)
		//		throw new ArgumentNullException(nameof(strategy));

		//	return strategy.Security.GetMarketPrice(strategy.SafeGetConnector(), side);
		//}

		#region Strategy rules

		private abstract class StrategyRule<TArg> : MarketRule<Strategy, TArg>
		{
			protected StrategyRule(Strategy strategy)
				: base(strategy)
			{
				Strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
			}

			protected Strategy Strategy { get; }
		}

		private sealed class PnLManagerStrategyRule : StrategyRule<decimal>
		{
			private readonly Func<decimal, bool> _changed;

			public PnLManagerStrategyRule(Strategy strategy)
				: this(strategy, v => true)
			{
				Name = LocalizedStrings.PnLChange;
			}

			public PnLManagerStrategyRule(Strategy strategy, Func<decimal, bool> changed)
				: base(strategy)
			{
				_changed = changed ?? throw new ArgumentNullException(nameof(changed));

				Strategy.PnLChanged += OnPnLChanged;
			}

			private void OnPnLChanged()
			{
				if (_changed(Strategy.PnL))
					Activate(Strategy.PnL);
			}

			protected override void DisposeManaged()
			{
				Strategy.PnLChanged -= OnPnLChanged;
				base.DisposeManaged();
			}
		}

		private sealed class PositionManagerStrategyRule : StrategyRule<decimal>
		{
			private readonly Func<decimal, bool> _changed;

			public PositionManagerStrategyRule(Strategy strategy)
				: this(strategy, v => true)
			{
				Name = LocalizedStrings.Positions;
			}

			public PositionManagerStrategyRule(Strategy strategy, Func<decimal, bool> changed)
				: base(strategy)
			{
				_changed = changed ?? throw new ArgumentNullException(nameof(changed));

				Strategy.PositionChanged += OnPositionChanged;
			}

			private void OnPositionChanged()
			{
				if (_changed(Strategy.Position))
					Activate(Strategy.Position);
			}

			protected override void DisposeManaged()
			{
				Strategy.PositionChanged -= OnPositionChanged;
				base.DisposeManaged();
			}
		}

		private sealed class NewMyTradeStrategyRule : StrategyRule<MyTrade>
		{
			public NewMyTradeStrategyRule(Strategy strategy)
				: base(strategy)
			{
				Name = LocalizedStrings.NewTrades + " " + strategy;
				Strategy.NewMyTrade += OnStrategyNewMyTrade;
			}

			private void OnStrategyNewMyTrade(MyTrade trade)
			{
				Activate(trade);
			}

			protected override void DisposeManaged()
			{
				Strategy.NewMyTrade -= OnStrategyNewMyTrade;
				base.DisposeManaged();
			}
		}

		private sealed class OrderRegisteredStrategyRule : StrategyRule<Order>
		{
			public OrderRegisteredStrategyRule(Strategy strategy)
				: base(strategy)
			{
				Name = LocalizedStrings.Orders + " " + strategy;
				Strategy.OrderRegistered += Activate;
			}

			protected override void DisposeManaged()
			{
				Strategy.OrderRegistered -= Activate;
				base.DisposeManaged();
			}
		}

		private sealed class OrderChangedStrategyRule : StrategyRule<Order>
		{
			public OrderChangedStrategyRule(Strategy strategy)
				: base(strategy)
			{
				Name = LocalizedStrings.Orders + " " + strategy;
				Strategy.OrderChanged += Activate;
			}

			protected override void DisposeManaged()
			{
				Strategy.OrderChanged -= Activate;
				base.DisposeManaged();
			}
		}

		private sealed class ProcessStateChangedStrategyRule : StrategyRule<Strategy>
		{
			private readonly Func<ProcessStates, bool> _condition;

			public ProcessStateChangedStrategyRule(Strategy strategy, Func<ProcessStates, bool> condition)
				: base(strategy)
			{
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));

				Strategy.ProcessStateChanged += OnProcessStateChanged;
			}

			private void OnProcessStateChanged(Strategy strategy)
			{
				if (_condition(Strategy.ProcessState))
					Activate(Strategy);
			}

			protected override void DisposeManaged()
			{
				Strategy.ProcessStateChanged -= OnProcessStateChanged;
				base.DisposeManaged();
			}
		}

		private sealed class PropertyChangedStrategyRule : StrategyRule<Strategy>
		{
			private readonly Func<Strategy, bool> _condition;

			public PropertyChangedStrategyRule(Strategy strategy, Func<Strategy, bool> condition)
				: base(strategy)
			{
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));

				Strategy.PropertyChanged += OnPropertyChanged;
			}

			private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
			{
				if (_condition(Strategy))
					Activate(Strategy);
			}

			protected override void DisposeManaged()
			{
				Strategy.PropertyChanged -= OnPropertyChanged;
				base.DisposeManaged();
			}
		}

		private sealed class ErrorStrategyRule : StrategyRule<Exception>
		{
			private readonly bool _processChildStrategyErrors;

			public ErrorStrategyRule(Strategy strategy, bool processChildStrategyErrors)
				: base(strategy)
			{
				_processChildStrategyErrors = processChildStrategyErrors;

				Name = strategy + LocalizedStrings.Error;
				Strategy.Error += OnError;
			}

			private void OnError(Strategy strategy, Exception error)
			{
				if (!_processChildStrategyErrors && !Equals(Strategy, strategy))
					return;

				Activate(error);
			}

			protected override void DisposeManaged()
			{
				Strategy.Error -= OnError;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of occurrence new strategy trade.
		/// </summary>
		/// <param name="strategy">The strategy, based on which trade occurrence will be traced.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, MyTrade> WhenNewMyTrade(this Strategy strategy)
		{
			return new NewMyTradeStrategyRule(strategy);
		}

		/// <summary>
		/// To create a rule for event of occurrence of new strategy order.
		/// </summary>
		/// <param name="strategy">The strategy, based on which order occurrence will be traced.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, Order> WhenOrderRegistered(this Strategy strategy)
		{
			return new OrderRegisteredStrategyRule(strategy);
		}

		/// <summary>
		/// To create a rule for event of change of any strategy order.
		/// </summary>
		/// <param name="strategy">The strategy, based on which orders change will be traced.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, Order> WhenOrderChanged(this Strategy strategy)
		{
			return new OrderChangedStrategyRule(strategy);
		}

		/// <summary>
		/// To create a rule for the event of strategy position change.
		/// </summary>
		/// <param name="strategy">The strategy, based on which position change will be traced.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, decimal> WhenPositionChanged(this Strategy strategy)
		{
			return new PositionManagerStrategyRule(strategy);
		}

		/// <summary>
		/// To create a rule for event of position event reduction below the specified level.
		/// </summary>
		/// <param name="strategy">The strategy, based on which position change will be traced.</param>
		/// <param name="value">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, decimal> WhenPositionLess(this Strategy strategy, Unit value)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			if (value == null)
				throw new ArgumentNullException(nameof(value));

			var finishPosition = value.Type == UnitTypes.Limit ? value : strategy.Position - value;

			return new PositionManagerStrategyRule(strategy, pos => pos < finishPosition)
			{
				Name = $"Pos < {value}"
			};
		}

		/// <summary>
		/// To create a rule for event of position event increase above the specified level.
		/// </summary>
		/// <param name="strategy">The strategy, based on which position change will be traced.</param>
		/// <param name="value">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, decimal> WhenPositionMore(this Strategy strategy, Unit value)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			if (value == null)
				throw new ArgumentNullException(nameof(value));

			var finishPosition = value.Type == UnitTypes.Limit ? value : strategy.Position + value;

			return new PositionManagerStrategyRule(strategy, pos => pos > finishPosition)
			{
				Name = $"Pos > {value}"
			};
		}

		/// <summary>
		/// To create a rule for event of profit reduction below the specified level.
		/// </summary>
		/// <param name="strategy">The strategy, based on which the profit change will be traced.</param>
		/// <param name="value">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, decimal> WhenPnLLess(this Strategy strategy, Unit value)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			if (value == null)
				throw new ArgumentNullException(nameof(value));

			var finishPosition = value.Type == UnitTypes.Limit ? value : strategy.PnL - value;

			return new PnLManagerStrategyRule(strategy, pos => pos < finishPosition)
			{
				Name = $"P&L < {value}"
			};
		}

		/// <summary>
		/// To create a rule for event of profit increase above the specified level.
		/// </summary>
		/// <param name="strategy">The strategy, based on which the profit change will be traced.</param>
		/// <param name="value">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, decimal> WhenPnLMore(this Strategy strategy, Unit value)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			if (value == null)
				throw new ArgumentNullException(nameof(value));

			var finishPosition = value.Type == UnitTypes.Limit ? value : strategy.PnL + value;

			return new PnLManagerStrategyRule(strategy, pos => pos > finishPosition)
			{
				Name = $"P&L > {value}"
			};
		}

		/// <summary>
		/// To create a rule for event of profit change.
		/// </summary>
		/// <param name="strategy">The strategy, based on which the profit change will be traced.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, decimal> WhenPnLChanged(this Strategy strategy)
		{
			return new PnLManagerStrategyRule(strategy);
		}

		/// <summary>
		/// To create a rule for event of start of strategy operation.
		/// </summary>
		/// <param name="strategy">The strategy, based on which the start of strategy operation will be expected.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, Strategy> WhenStarted(this Strategy strategy)
		{
			return new ProcessStateChangedStrategyRule(strategy, s => s == ProcessStates.Started)
			{
				Name = strategy + LocalizedStrings.Started,
			};
		}

		/// <summary>
		/// To create a rule for event of beginning of the strategy operation stop.
		/// </summary>
		/// <param name="strategy">The strategy, based on which the beginning of stop will be determined.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, Strategy> WhenStopping(this Strategy strategy)
		{
			return new ProcessStateChangedStrategyRule(strategy, s => s == ProcessStates.Stopping)
			{
				Name = strategy + LocalizedStrings.Stopping,
			};
		}

		/// <summary>
		/// To create a rule for event full stop of strategy operation.
		/// </summary>
		/// <param name="strategy">The strategy, based on which the full stop will be expected.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, Strategy> WhenStopped(this Strategy strategy)
		{
			return new ProcessStateChangedStrategyRule(strategy, s => s == ProcessStates.Stopped)
			{
				Name = strategy + LocalizedStrings.Stopped,
			};
		}

		/// <summary>
		/// To create a rule for event of strategy error (transition of state <see cref="Strategy.ErrorState"/> into <see cref="LogLevels.Error"/>).
		/// </summary>
		/// <param name="strategy">The strategy, based on which error will be expected.</param>
		/// <param name="processChildStrategyErrors">Process the child strategies errors.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, Exception> WhenError(this Strategy strategy, bool processChildStrategyErrors = false)
		{
			return new ErrorStrategyRule(strategy, processChildStrategyErrors);
		}

		/// <summary>
		/// To create a rule for event of strategy warning (transition of state <see cref="Strategy.ErrorState"/> into <see cref="LogLevels.Warning"/>).
		/// </summary>
		/// <param name="strategy">The strategy, based on which the warning will be expected.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, Strategy> WhenWarning(this Strategy strategy)
		{
			return new PropertyChangedStrategyRule(strategy, s => s.ErrorState == LogLevels.Warning)
			{
				Name = strategy + LocalizedStrings.Warning,
			};
		}

		#endregion

		#region Order actions

		/// <summary>
		/// To create an action, registering the order.
		/// </summary>
		/// <param name="rule">Rule.</param>
		/// <param name="order">The order to be registered.</param>
		/// <returns>Rule.</returns>
		public static IMarketRule Register(this IMarketRule rule, Order order)
		{
			if (rule == null)
				throw new ArgumentNullException(nameof(rule));

			if (order == null)
				throw new ArgumentNullException(nameof(order));

			return rule.Do(() => GetRuleStrategy(rule).RegisterOrder(order));
		}

		/// <summary>
		/// To create an action, re-registering the order.
		/// </summary>
		/// <param name="rule">Rule.</param>
		/// <param name="oldOrder">The order to be re-registered.</param>
		/// <param name="newOrder">Information about new order.</param>
		/// <returns>Rule.</returns>
		public static IMarketRule ReRegister(this IMarketRule rule, Order oldOrder, Order newOrder)
		{
			if (rule == null)
				throw new ArgumentNullException(nameof(rule));

			if (oldOrder == null)
				throw new ArgumentNullException(nameof(oldOrder));

			if (newOrder == null)
				throw new ArgumentNullException(nameof(newOrder));

			return rule.Do(() => GetRuleStrategy(rule).ReRegisterOrder(oldOrder, newOrder));
		}

		/// <summary>
		/// To create an action, cancelling the order.
		/// </summary>
		/// <param name="rule">Rule.</param>
		/// <param name="order">The order to be cancelled.</param>
		/// <returns>Rule.</returns>
		public static IMarketRule Cancel(this IMarketRule rule, Order order)
		{
			if (rule == null)
				throw new ArgumentNullException(nameof(rule));

			if (order == null)
				throw new ArgumentNullException(nameof(order));

			return rule.Do(() => GetRuleStrategy(rule).CancelOrder(order));
		}

		#endregion

		private static Strategy GetRuleStrategy(IMarketRule rule)
		{
			if (rule == null)
				throw new ArgumentNullException(nameof(rule));

			if (rule.Container is not Strategy strategy)
				throw new ArgumentException(LocalizedStrings.RuleNotRegisteredInStrategy.Put(rule), nameof(rule));

			return strategy;
		}

		/// <summary>
		/// To open the position via quoting.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="finishPosition">The position value that should be reached. A negative value means the short position.</param>
		public static void OpenPositionByQuoting(this Strategy strategy, decimal finishPosition)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			var position = strategy.Position;

			if (finishPosition == position)
				return;

			var delta = (finishPosition - position).Abs();

			var quoting = new MarketQuotingStrategy(finishPosition < position ? Sides.Sell : Sides.Buy, delta);
			strategy.ChildStrategies.Add(quoting);
		}

		/// <summary>
		/// To close the open position via quoting.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		public static void ClosePositionByQuoting(this Strategy strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			var position = strategy.Position;

			if (position == 0)
				return;

			var quoting = new MarketQuotingStrategy(position > 0 ? Sides.Sell : Sides.Buy, position.Abs());
			strategy.ChildStrategies.Add(quoting);
		}

		///// <summary>
		///// To create the action protecting the order by strategies <see cref="TakeProfitStrategy"/> and <see cref="StopLossStrategy"/>.
		///// </summary>
		///// <param name="rule">The rule associated with the order.</param>
		///// <param name="takePriceDelta">The delta from the price of the protected order, by which the protective take profit order is to be registered.</param>
		///// <param name="stopPriceDelta">The delta from the price of the protected order, by which the protective stop loss order is to be registered.</param>
		///// <returns>Rule.</returns>
		//public static MarketRule<Order, Order> Protect(this MarketRule<Order, Order> rule, Unit takePriceDelta, Unit stopPriceDelta)
		//{
		//	if (rule == null)
		//		throw new ArgumentNullException(nameof(rule));

		//	return rule.Do(order =>
		//		order
		//			.WhenNewTrades()
		//			.Protect(takePriceDelta, stopPriceDelta)
		//			.Apply(GetRuleStrategy(rule)));
		//}

		/// <summary>
		/// To create the action protecting orders by strategies <see cref="TakeProfitStrategy"/> and <see cref="StopLossStrategy"/>.
		/// </summary>
		/// <param name="rule">The rule for new orders.</param>
		/// <param name="takePriceDelta">The delta from the price of the protected order, by which the protective take profit order is to be registered.</param>
		/// <param name="stopPriceDelta">The delta from the price of the protected order, by which the protective stop loss order is to be registered.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, MyTrade> Protect(this MarketRule<Order, MyTrade> rule, Unit takePriceDelta, Unit stopPriceDelta)
		{
			return rule.Protect(
				takePriceDelta == null
					? null
					: t => new TakeProfitStrategy(t, takePriceDelta),
				stopPriceDelta == null
					? null
					: t => new StopLossStrategy(t, stopPriceDelta));
		}

		/// <summary>
		/// To create the action protecting orders by strategies <see cref="TakeProfitStrategy"/> and <see cref="StopLossStrategy"/>.
		/// </summary>
		/// <param name="rule">The rule for new orders.</param>
		/// <param name="takeProfit">The function that creates the strategy <see cref="TakeProfitStrategy"/> by the order.</param>
		/// <param name="stopLoss">The function that creates the strategy <see cref="StopLossStrategy"/> by the order.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, MyTrade> Protect(this MarketRule<Order, MyTrade> rule, Func<MyTrade, TakeProfitStrategy> takeProfit, Func<MyTrade, StopLossStrategy> stopLoss)
		{
			if (rule == null)
				throw new ArgumentNullException(nameof(rule));

			if (takeProfit == null && stopLoss == null)
				throw new ArgumentException(LocalizedStrings.NoTakeAndStop);

			Strategy CreateProtection(MyTrade trade)
			{
				if (takeProfit != null && stopLoss != null)
					return new TakeProfitStopLossStrategy(takeProfit(trade), stopLoss(trade));

				if (takeProfit != null)
					return takeProfit(trade);
				else
					return stopLoss(trade);
			}

			rule.Do(trade => GetRuleStrategy(rule).ChildStrategies.Add(CreateProtection(trade)));

			return rule;
		}

		private sealed class ActivatedStrategyRule : MarketRule<ProtectiveStrategy, ProtectiveStrategy>
		{
			private readonly ProtectiveStrategy _strategy;

			public ActivatedStrategyRule(ProtectiveStrategy strategy)
				: base(strategy)
			{
				Name = LocalizedStrings.Activation;

				_strategy = strategy;
				_strategy.Activated += Activate;
			}

			protected override void DisposeManaged()
			{
				_strategy.Activated -= Activate;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create the rule for the event <see cref="ProtectiveStrategy.Activated"/>.
		/// </summary>
		/// <param name="strategy">The strategy, by which the event will be monitored.</param>
		/// <returns>Rule.</returns>
		public static IMarketRule WhenActivated(this ProtectiveStrategy strategy)
		{
			return new ActivatedStrategyRule(strategy);
		}

		/// <summary>
		/// <see cref="Strategy.IsFormed"/> and <see cref="Strategy.TradingMode"/>.
		/// </summary>
		/// <param name="strategy"><see cref="Strategy"/></param>
		/// <param name="required">Required action.</param>
		/// <returns>Check result.</returns>
		public static bool IsFormedAndOnlineAndAllowTrading(this Strategy strategy, StrategyTradingModes required = StrategyTradingModes.Full)
		{
			if (strategy is null)
				throw new ArgumentNullException(nameof(strategy));

			if (!strategy.IsFormed || !strategy.IsOnline || strategy.TradingMode == StrategyTradingModes.Disabled)
				return false;

			return required switch
			{
				StrategyTradingModes.Full => strategy.TradingMode == StrategyTradingModes.Full,
				StrategyTradingModes.CancelOrdersOnly => true,
				StrategyTradingModes.ReducePositionOnly => strategy.TradingMode != StrategyTradingModes.CancelOrdersOnly,
				_ => throw new ArgumentOutOfRangeException(nameof(required), required, LocalizedStrings.InvalidValue),
			};
		}
	}
}