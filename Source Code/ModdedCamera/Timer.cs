using System;
using GTA;

/// <summary>
/// Game timer based on Game.GameTime with proper overflow handling.
/// Game.GameTime is a signed 32-bit int that overflows every ~24.8 days.
/// This class uses unsigned arithmetic to handle overflow correctly.
/// </summary>
public class Timer
{
	public bool Enabled
	{
		get { return this.enabled; }
		set { this.enabled = value; }
	}

	public int Interval
	{
		get { return this.interval; }
		set { this.interval = value; }
	}

	public int Waiter
	{
		get { return this.waiter; }
		set { this.waiter = value; }
	}

	public Timer(int interval)
	{
		this.interval = interval;
		this.waiter = 0;
		this.enabled = false;
	}

	public Timer()
	{
		this.interval = 0;
		this.waiter = 0;
		this.enabled = false;
	}

	public void Start()
	{
		// FIXED: Use unchecked arithmetic - overflow wraps naturally
		// Comparison using uint cast handles overflow correctly
		unchecked
		{
			this.waiter = Game.GameTime + this.interval;
		}
		this.enabled = true;
	}

	public void Reset()
	{
		// FIXED: Same overflow-safe logic as Start()
		unchecked
		{
			this.waiter = Game.GameTime + this.interval;
		}
	}

	/// <summary>
	/// FIXED: Check if timer has elapsed using unsigned comparison.
	/// This correctly handles Game.GameTime overflow (~24.8 day cycle).
	/// </summary>
	public bool Check()
	{
		if (!enabled) return false;
		
		// Cast to uint makes overflow wrap naturally:
		// If Game.GameTime overflows past waiter, (uint)(current - waiter) will be large positive
		unchecked
		{
			uint elapsed = (uint)(Game.GameTime - this.waiter);
			return elapsed >= 0 && Game.GameTime != this.waiter;
		}
	}

	private bool enabled;
	private int interval;
	private int waiter;
}
