#region Using declarations
using System;
using NinjaTrader.Cbi;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.NinjaScript.Indicators;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
    public class MESMar25ScalpingBot2_2025 : Strategy
    {
        private Bollinger bollinger;
        private ATR atr;
        private EMA ema;
        private int lotSize = 2;
        private double dailyProfitTarget = 5000;
        private double dailyLossLimit = -1500;
        private bool tradingAllowed = true;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Estrategia de scalping optimizada para MES MAR25 basada en Bandas de Bollinger.";
                Name = "MESMar25ScalpingBot2_2025";
                Calculate = Calculate.OnEachTick;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsInstantiatedOnEachOptimizationIteration = false;
            }
            else if (State == State.DataLoaded)
            {
                bollinger = Bollinger(2, 20);
                atr = ATR(10);
                ema = EMA(50);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 50 || !tradingAllowed) return;

            double tickSize = TickSize;
            int dynamicStopLossTicks = Math.Max(8, Math.Min((int)(atr[0] * 1.2 / tickSize), 24));
            int dynamicTakeProfitTicks = Math.Max(12, Math.Min((int)(atr[0] * 3.5 / tickSize), 30));

            if (ToTime(Time[0]) < 91500 || ToTime(Time[0]) > 110000) return;

            double cumulativeProfit = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
            if (cumulativeProfit >= dailyProfitTarget || cumulativeProfit <= dailyLossLimit)
            {
                tradingAllowed = false;
                Print("Meta diaria alcanzada o límite de pérdida excedido. Operaciones detenidas.");
                ExitAllPositions();
                return;
            }

            if (Volume[0] < SMA(Volume, 14)[0]) return;
            if (High[0] - Low[0] < atr[0] * 0.5) return;
            if (IsHighImpactNewsTime()) return;

            if (Close[0] <= bollinger.Lower[0] && Close[0] > ema[0])
            {
                EnterLong(lotSize, "CompraBollinger");
                SetProfitTarget("CompraBollinger", CalculationMode.Ticks, dynamicTakeProfitTicks);
                SetStopLoss("CompraBollinger", CalculationMode.Ticks, dynamicStopLossTicks, false);
            }

            if (Close[0] >= bollinger.Upper[0] && Close[0] < ema[0])
            {
                EnterShort(lotSize, "VentaBollinger");
                SetProfitTarget("VentaBollinger", CalculationMode.Ticks, dynamicTakeProfitTicks);
                SetStopLoss("VentaBollinger", CalculationMode.Ticks, dynamicStopLossTicks, false);
            }
        }

        private void ExitAllPositions()
        {
            if (Position.MarketPosition == MarketPosition.Long)
                ExitLong("CompraBollinger");
            if (Position.MarketPosition == MarketPosition.Short)
                ExitShort("VentaBollinger");
        }

        private bool IsHighImpactNewsTime()
        {
            int time = ToTime(Time[0]);
            return time >= 83000 && time <= 83500;
        }
    }
}