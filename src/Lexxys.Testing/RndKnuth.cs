using System;

using Lexxys;

namespace Lexxys.Testing;

/// <summary>
/// Implementation of pseudo-random numbers generator by DEK.
/// </summary>
public class RndKnuth: IRand
{
	/*    This program by D E Knuth is in the public domain and freely copyable
	 *    AS LONG AS YOU MAKE ABSOLUTELY NO CHANGES!
	 *    It is explained in Seminumerical Algorithms, 3rd edition, Section 3.6
	 *    (or in the errata to the 2nd edition --- see
	 *        http://www-cs-faculty.stanford.edu/~knuth/taocp.html
	 *    in the changes to Volume 2 on pages 171 and following).              */

	/*    N.B. The MODIFICATIONS introduced in the 9th printing (2002) are
		  included here; there's no backwards compatibility with the original. */

	/*    This version also adopts Brendan McKay's suggestion to
		  accommodate naive users who forget to call ran_start(seed).          */

	/*    If you find any bugs, please report them immediately to
	 *                 taocp@cs.stanford.edu
	 *    (and you will be rewarded if the bug is genuine). Thanks!            */

	/************ see the book for explanations and caveats! *******************/
	/************ in particular, you need two's complement arithmetic **********/

	static int ModDiff(int x, int y) => (x - y) & (MM - 1);                /* subtraction mod MM */
	static double ModSum(double x, double y) => (x + y) - (int)(x + y);    /* (x+y) mod 1.0 */
	static bool IsOdd(long x) => (x & 1) == 1;						/* units bit of x */

	const int QUALITY = 1009;										/* recommended quality level for high-res use */
	const int KK = 100;												/* the long lag */
	const int LL = 37;                                              /* the short lag */
	readonly int TT = 70;											/* guaranteed separation between streams */
	const int MM = (1<<30);											/* the modulus */
	const int ran_arr_started = KK;

	readonly int[] ran_x = new int[KK];                             /* the generator state */
	readonly int[] ran_arr_buf = new int[QUALITY];
	int ran_arr_ptr = ran_arr_started; //&ran_arr_dummy;			/* the next random number, or -1 */
	bool _integerInitialized;
	bool _doubleInitialized;
	int _seed;
	readonly double[] ranf_arr_buf = new double[QUALITY];
	int ranf_arr_ptr = ran_arr_started;								/* the next random fraction, or -1 */
	private readonly double[] ran_u = new double[KK];				/* the generator state */

	private readonly object syncObj = new object();

	public RndKnuth(int seed = 0)
	{
		ran_arr_buf[KK] = -1;
		ranf_arr_buf[KK] = -1;
		Reset(seed);
	}

	private void InitLongArray(int[] aa, int n)						/* put n new random numbers in aa */
	{
		int i, j;
		for (j = 0; j < KK; j++)
			aa[j] = ran_x[j];
		for (; j < n; j++)
			aa[j] = ModDiff(aa[j-KK], aa[j - LL]);
		for (i=0; i < LL; i++, j++)
			ran_x[i] = ModDiff(aa[j - KK], aa[j - LL]);
		for (; i < KK; i++, j++)
			ran_x[i] = ModDiff(aa[j - KK], ran_x[i - LL]);
	}

	/* the following routines are from exercise 3.6--15 */
	/* after calling ran_start, get new randoms by, e.g., "x=ran_arr_next()" */

	#region IRand

	void IRand.Reset(int seed)
	{
		lock (syncObj)
		{
			Reset(seed);
		}
	}

	int IRand.NextInt()
	{
		lock (syncObj)
		{
			return NextInt();
		}
	}

	double IRand.NextDouble()
	{
		lock (syncObj)
		{
			return NextDouble();
		}
	}

	void IRand.NextBytes(byte[] buffer)
	{
		lock (syncObj)
		{
			NextBytes(buffer);
		}
	}

	#endregion

	public void Reset(int seed = 0)
	{
		_seed = seed <= 0 ? (int)(DateTime.Now.Ticks & 0x3FFFFFFF): (seed  & 0x3FFFFFFF);
		ran_arr_ptr = ran_arr_started;
		ranf_arr_ptr = ran_arr_started;
		_integerInitialized = false;
		_doubleInitialized = false;
	}

	private void StartInt(int seed)   /* do this before using ran_array */
								//		  long seed;            /* selector for different streams */
	{
		_integerInitialized = true;
		int t, j;
		int[] x = new int[KK + KK - 1];              /* the preparation buffer */
		int ss = (seed + 2) & (MM - 2);
		for (j = 0; j < KK; j++)
		{
			x[j] = ss;									/* bootstrap the buffer */
			ss<<=1;
			if (ss >= MM)								/* cyclic shift 29 bits */
				ss -= MM-2;
		}
		x[1]++;											/* make x[1] (and only x[1]) odd */
		for (ss=seed & (MM-1), t = TT-1; t != 0;)
		{
			for (j = KK-1; j > 0; j--)
			{
				x[j+j] = x[j];
				x[j+j-1] = 0;
			} /* "square" */
			for (j = KK+KK-2; j >= KK; j--)
			{
				x[j-(KK-LL)] = ModDiff(x[j-(KK-LL)], x[j]);
				x[j-KK] = ModDiff(x[j-KK], x[j]);
			}
			if (IsOdd(ss))
			{              /* "multiply by z" */
				for (j = KK; j > 0; j--)
					x[j]=x[j-1];
				x[0] = x[KK];            /* shift the buffer cyclically */
				x[LL] = ModDiff(x[LL], x[KK]);
			}
			if (ss != 0)
				ss >>= 1;
			else
				t--;
		}
		for (j = 0; j < LL; j++)
			ran_x[j+KK-LL] = x[j];
		for (; j < KK; j++)
			ran_x[j-LL] = x[j];
		for (j = 0; j < 10; j++)
			InitLongArray(x, KK + KK - 1); /* warm things up */

		ran_arr_ptr = ran_arr_started;
	}

	/// <inheritdoc/>
	public int NextInt()
	{
		var x = ran_arr_buf[ran_arr_ptr++];
		if (x < 0)
			x = CycleLong();
		else
			++ranf_arr_ptr;
		return x;
	}

	private int CycleLong()
	{
		if (!_integerInitialized)
			StartInt(_seed);
		InitLongArray(ran_arr_buf, QUALITY);
		ran_arr_buf[KK] = -1;
		ran_arr_ptr = 1;
		return ran_arr_buf[0];
	}

	private void InitDoubleArray(double[] aa, int n) /* put n new random fractions in aa */
	{
	  int i, j;
	  for (j=0;j<KK;j++)
			aa[j]=ran_u[j];
	  for (;j<n;j++)
			aa[j]=ModSum(aa[j-KK], aa[j-LL]);
	  for (i=0;i<LL;i++,j++)
			ran_u[i]=ModSum(aa[j-KK], aa[j-LL]);
	  for (;i<KK;i++,j++)
			ran_u[i]=ModSum(aa[j-KK], ran_u[i-LL]);
	}

	private void StartDouble(int seed)
	{
		_doubleInitialized = true;
		int t, s, j;
		double[] u = new double[KK + KK - 1];
		double ulp = (1.0/(1L<<30))/(1L<<22);               /* 2 to the -52 */
		double ss = 2.0*ulp*((seed & 0x3fffffff)+2);

		for (j=0; j<KK; j++)
		{
			u[j]=ss;                                /* bootstrap the buffer */
			ss+=ss;
			if (ss >= 1.0)
				ss -= 1.0-2* ulp;			/* cyclic shift of 51 bits */
		}
		u[1] += ulp;						/* make u[1] (and only u[1]) "odd" */
		for (s=seed & 0x3fffffff, t = TT-1; t != 0;)
		{
			for (j=KK-1; j>0; j--)		/* "square" */
			{
				u[j+j] = u[j];
				u[j+j-1] = 0.0;
			}

			for (j=KK+KK-2; j>=KK; j--)
			{
				u[j-(KK-LL)] = ModSum(u[j-(KK-LL)], u[j]);
				u[j-KK] = ModSum(u[j-KK], u[j]);
			}
			if (IsOdd(s))
			{                             /* "multiply by z" */
				for (j=KK; j>0; j--)
					u[j]=u[j-1];
				u[0]=u[KK];                    /* shift the buffer cyclically */
				u[LL] = ModSum(u[LL], u[KK]);
			}
			if (s != 0) s>>=1; else t--;
		}
		for (j=0; j<LL; j++)
			ran_u[j+KK-LL]=u[j];
		for (; j<KK; j++)
			ran_u[j-LL] = u[j];
		for (j=0; j<10; j++)
			InitDoubleArray(u, KK + KK - 1);  /* warm things up */

		ranf_arr_ptr = ran_arr_started;
	}

	/// <inheritdoc/>
	public double NextDouble()
	{
		var x = ranf_arr_buf[ranf_arr_ptr];
		if (x < 0)
			x = CycleDouble();
		else
			++ranf_arr_ptr;
		return x;
	}

	private double CycleDouble()
	{
		if (!_doubleInitialized)
			StartDouble(_seed);
		InitDoubleArray(ranf_arr_buf, QUALITY);
		ranf_arr_buf[KK] = -1;
		ranf_arr_ptr = 1;
		return ranf_arr_buf[0];
	}

	/// <inheritdoc/>
	public unsafe void NextBytes(byte[] buffer)
	{
		if (buffer is null)
			throw new ArgumentNullException(nameof(buffer));

		fixed (byte* b = buffer)
		{
			byte* p = b;
			int n = buffer.Length;
			while (n >= sizeof(int))
			{
				*(int*)p = NextInt();
				p += sizeof(int) - 1;
				n -= sizeof(int) - 1;
			}
			if (n > 0)
			{
				byte* x = stackalloc byte[sizeof(int)];
				*(int*)x = NextInt();
				while (n > 0)
				{
					*p++ = *x++;
					--n;
				}
			}
		}
	}
}
