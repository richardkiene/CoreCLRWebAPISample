# CoreCLRWebAPISample
Sample CoreCLR WebAPI application used for trivial benchmarking

* CoreCLR performance on SmartOS LX branded Zones is _AWESOME_. This
repo is here to document the steps used for benchmarking, list current and
historical benchmark numbers, list current findings / progress, and hopefully
lower the barrier to entry so others can join in the fun.

* The sample WebAPI application included is about as simple as possible. The only
route available is `GET /values/{id}`, which returns the integer supplied to the
route. It's possible that other samples will need to be built, but currently
this sample demonstrates the slowness while keeping complexity and scope narrow.

## Setup
* Create an LX branded zone (preferably Ubuntu 14.04)
    * If you want the fastest route use [Triton](https://www.joyent.com)
    * If you prefer your own gear use [SmartOS](https://wiki.smartos.org/display/DOC/LX+Branded+Zones)
* Follow the instructions for [setting up dnx](http://docs.asp.net/en/latest/getting-started/installing-on-linux.html).
* Install [wrk benchmark](https://github.com/wg/wrk/wiki/Installing-Wrk-on-Linux) (preferably on a different VM on the same L2 network)

## Running
* On machine #1
```
$ git clone git@github.com:richardkiene/CoreCLRWebAPISample.git
$ cd CoreCLRWebAPISample
$ dnu restore
$ dnx web
```

* On machine #2
```
$./wrk -t8 -c32 -d30s http://192.168.1.2:8080/values/5
```

## Current benchmarks (w/ comparisons to other envs)
#### UPDATE 1:
*CoreCLR 1.0.0-rc2-16308 contains a partial fix for lock contention. The reason the VMWare Ubuntu stats look better is due to core count (2 vs 48 on the LX machine). More cores equates to more locking. 1.0.0-rc2-16308 has been added to the LX benchmark, but more details are on the way.*

#### UPDATE 2:
This [PR](https://github.com/dotnet/corefx/pull/5027) removes ICU
locking from the hot path and makes things dramatically better, results are below.

#### CoreCLR 1.0.0-rc2-16308 + master build System.ComponentModel.TypeConverter on LX Ubuntu 14.04 (Joyent Public Cloud)
```
wrk -t8 -c32 -d30s http://192.168.128.8:8080/values/5
Running 30s test @ http://192.168.128.8:8080/values/5
  8 threads and 32 connections
  Thread Stats   Avg      Stdev     Max   +/- Stdev
    Latency     3.35ms    4.47ms  71.39ms   89.70%
    Req/Sec     1.84k   225.02     2.36k    69.33%
  439021 requests in 30.02s, 64.06MB read
Requests/sec:  14625.69
Transfer/sec:      2.13MB
```

#### CoreCLR 1.0.0-rc2-16308 on LX Ubuntu 14.04 (Joyent Public Cloud)
```
$ wrk -t8 -c32 -d30s http://192.168.128.13:8080/values/5
Running 30s test @ http://192.168.128.13:8080/values/5
  8 threads and 32 connections
  Thread Stats   Avg      Stdev     Max   +/- Stdev
    Latency     4.52ms    7.56ms  98.51ms   90.60%
    Req/Sec     1.63k   298.02     2.31k    80.29%
  389026 requests in 30.02s, 56.76MB read
Requests/sec:  12959.73
Transfer/sec:      1.89MB
```

#### CoreCLR 1.0.0-rc1-update1 on LX Ubuntu 14.04 (Joyent Public Cloud)
```
#
$ wrk -t8 -c32 -d30s http://192.168.128.8:8080/values/5
Running 30s test @ http://192.168.128.8:8080/values/5
  8 threads and 32 connections
  Thread Stats   Avg      Stdev     Max   +/- Stdev
    Latency    30.88ms   26.91ms 201.56ms   32.02%
    Req/Sec   138.74     44.26   333.00     70.38%
  33205 requests in 30.03s, 4.98MB read
Requests/sec:   1105.56
Transfer/sec:    169.65KB
```

#### Node.js on LX Ubuntu 14.04 (Joyent Public Cloud)
```
$ wrk -t8 -c32 -d30s http://192.168.128.8:8080/values/5
Running 30s test @ http://192.168.128.8:8080/values/5
  8 threads and 32 connections
  Thread Stats   Avg      Stdev     Max   +/- Stdev
    Latency     4.91ms    1.63ms  28.52ms   85.38%
    Req/Sec   821.73    219.58     1.53k    59.12%
  196524 requests in 30.04s, 26.80MB read
Requests/sec:   6541.38
Transfer/sec:      0.89MB
```
#### CoreCLR on Windows on VMWare Fusion on MacOSX
```
$ wrk -t8 -c32 -d30s http://10.88.88.130:8080/values/5
Running 30s test @ http://10.88.88.130:8080/values/5
  8 threads and 32 connections
  Thread Stats   Avg      Stdev     Max   +/- Stdev
    Latency     7.28ms    6.97ms 502.16ms   98.22%
    Req/Sec   568.03     96.86   820.00     64.75%
  136057 requests in 30.10s, 20.37MB read
Requests/sec:   4520.15
Transfer/sec:    693.03KB
```
#### CoreCLR on Ubuntu 14.04 on VMWare Fusion on MacOSX
```
$ wrk -t8 -c32 -d30s http://10.88.88.128:8080/values/5
Running 30s test @ http://10.88.88.128:8080/values/5
  8 threads and 32 connections
  Thread Stats   Avg      Stdev     Max   +/- Stdev
    Latency     8.24ms    1.38ms  24.39ms   68.95%
    Req/Sec   487.67     31.48   606.00     71.88%
  116725 requests in 30.06s, 17.48MB read
Requests/sec:   3882.55
Transfer/sec:    595.27KB
```
#### CoreCLR on MacOSX 10.11 native
```
$ wrk -t8 -c32 -d30s http://localhost:8080/values/5
Running 30s test @ http://localhost:8080/values/5
  8 threads and 32 connections
  Thread Stats   Avg      Stdev     Max   +/- Stdev
    Latency     4.06ms    2.63ms 163.33ms   98.21%
    Req/Sec     1.04k   108.83     1.24k    92.42%
  247489 requests in 30.02s, 36.11MB read
Requests/sec:   8243.79
Transfer/sec:      1.20MB
```

## Current dtrace / stack / OS metrics

* [core.74241](http://us-east.manta.joyent.com/shmeeny/public/core.74241) for use with mdb

* Create and analyze your own core file:
```
$ gcore `pgrep dnx`
$ mdb core.`pgrep dnx`
> ::stacks
> <address>::dis
```

## Current findings

* String comparisons are naturally made all over the place in MVC (both for
route handling and input / output formatting). Many of these are culture
sensitive unicode comparisons that call into ICU. In the RC1 release of CoreCLR,
collators were not cached and each string comparison created a new collator and
an expensive global lock. In the RC2 release of CoreCLR collators are cached,
but comparison operations still end up creating a global lock in ICU due to the
nature of ICU code. This is intended as a brief and incremental update, and more
information will be updated to this repo soon.

* Update the largest offender of non-ordinal string comparison has been
fixed in master. The performance increase is significant, and libicu doesn't
even show up in the user land stack anymore. See https://github.com/dotnet/corefx/pull/5027
for details. There are still some string comparisons that need to be ordinal but
they only impact startup time.

* Known CoreCLR hot spots: [collation.cpp](https://github.com/dotnet/coreclr/blob/15706ebfda035867c3435343d08c33dec579d5dc/src/corefx/System.Globalization.Native/collation.cpp#L502)

* Running [mytrace.d](https://gist.github.com/richardkiene/baaa15bbe7e5b8975045) yields:

#### OLD
```
PID             EXECNAME                 FUNC      COUNT
85636              dnx libicuuc.so.52`u_strlen_52         12
85636              dnx libcoreclr.so`_Z27JIT_GetGenericsGCStaticBaseP11MethodTable         13
85636              dnx libcoreclr.so`YieldProcessor         13
85636              dnx libpthread.so.0`__lll_unlock_wake         14
85636              dnx libcoreclr.so`_Z30JIT_NewArr1OBJ_MP_FastPortableP21CORINFO_CLASS_STRUCT_l         15
85636              dnx libcoreclr.so`_ZN7CorUnix28InternalEnterCriticalSectionEPNS_10CPalThreadEP17_CRITICAL_SECTION         16
85636              dnx libcoreclr.so`JIT_ByRefWriteBarrier         18
85636              dnx     libc.so.6`memset         19
85636              dnx libicui18n.so.52`usearch_openFromCollator_52         20
85636              dnx libpthread.so.0`pthread_mutex_unlock         21
85636              dnx libcoreclr.so`_Z18JIT_GetRuntimeTypeP21CORINFO_CLASS_STRUCT_         22
85636              dnx libcoreclr.so`JIT_WriteBarrier         22
85636              dnx libcoreclr.so`__vdso_clock_gettime         23
85636              dnx ld-linux-x86-64.so.2`__tls_get_addr         24
85636              dnx libpthread.so.0`pthread_mutex_lock         28
85636              dnx libcoreclr.so`_Z24JIT_NewS_MP_FastPortableP21CORINFO_CLASS_STRUCT_         30
85636              dnx libpthread.so.0`__lll_lock_wait         30
85636              dnx libcoreclr.so`_Z31JIT_GetSharedGCThreadStaticBasemj         35
85636              dnx libpthread.so.0`pthread_getspecific         36
85636              dnx libicui18n.so.52`ucol_prv_getSpecialCE_52         41
85636              dnx     libc.so.6`malloc         44
85636              dnx libcoreclr.so`_Z10ClrSleepExji         49
85636              dnx libicui18n.so.52`ucol_updateInternalState_52         61
85636              dnx libcoreclr.so`SleepEx         70
85636              dnx libcoreclr.so`_ZN13ThreadpoolMgr15UnfairSemaphore4WaitEj        218
```

#### NEW
```
PID             EXECNAME                 FUNC      COUNT
78290                  dnx libcoreclr.so`JIT_IsInstanceOfClass_Portable         70
78290                  dnx libcoreclr.so`JIT_MonExit_Portable         70
78290                  dnx libcoreclr.so`_ZN7CMiniMd12vSearchTableEj11CMiniColDefjPj         70
78290                  dnx libcoreclr.so`_ZN7CMiniMd22vSearchTableNotGreaterEj11CMiniColDefjPj         71
78290                  dnx libcoreclr.so`ObjIsInstanceOfNoGC         77
78290                  dnx libcoreclr.so`_Z30JIT_NewArr1OBJ_MP_FastPortableP21CORINFO_CLASS_STRUCT_l         86
78290                  dnx libcoreclr.so`JIT_CheckedWriteBarrier         88
78290                  dnx libcoreclr.so`JIT_GetSharedNonGCStaticBase_Portable         90
78290                  dnx libcoreclr.so`__vdso_clock_gettime         91
78290                  dnx libcoreclr.so`_ZN6Object25EnterObjMonitorHelperSpinEP6Thread         92
78290                  dnx libcoreclr.so`_Z29JIT_NewArr1VC_MP_FastPortableP21CORINFO_CLASS_STRUCT_l        101
78290                  dnx libcoreclr.so`_Z27JIT_GetGenericsGCStaticBaseP11MethodTable        119
78290                  dnx libcoreclr.so`JIT_ByRefWriteBarrier        128
78290                  dnx libcoreclr.so`JIT_MonReliableEnter_Portable        151
78290                  dnx libcoreclr.so`_Z18JIT_GetRuntimeTypeP21CORINFO_CLASS_STRUCT_        168
78290                  dnx libcoreclr.so`_ZN13ThreadpoolMgr15UnfairSemaphore4WaitEj        170
78290                  dnx libcoreclr.so`_ZN3WKS7gc_heap31mark_through_cards_for_segmentsEPFvPPhEi        179
78290                  dnx libcoreclr.so`JIT_WriteBarrier        183
78290                  dnx     libc.so.6`memset        196
78290                  dnx libcoreclr.so`_ZN3WKS7gc_heap17find_first_objectEPhS1_        227
78290                  dnx ld-linux-x86-64.so.2`__tls_get_addr        259
78290                  dnx libcoreclr.so`_Z24JIT_NewS_MP_FastPortableP21CORINFO_CLASS_STRUCT_        261
78290                  dnx libcoreclr.so`_Z31JIT_GetSharedGCThreadStaticBasemj        337
78290                  dnx libcoreclr.so`_ZN3WKSL15enter_spin_lockEPNS_15GCDebugSpinLockE       2118
78290                  dnx libcoreclr.so`YieldProcessor      13892

```

* `prstat -mLc 1` yields:
#### OLD
```
Total: 19 processes, 133 lwps, load averages: 0.25, 0.51, 0.38
   PID USERNAME USR SYS TRP TFL DFL LCK SLP LAT VCX ICX SCL SIG PROCESS/LWPID
 74241 root     4.9 4.9 0.0 0.0 0.0 0.0  90 0.5 917  15 11K   0 dnx/29
 74241 root     2.9 0.7 0.0 0.0 0.0  96 0.0 0.1 141   2  1K   0 dnx/81
 74241 root     2.4 0.9 0.0 0.0 0.0  97 0.0 0.1 132   4  1K   0 dnx/111
 74241 root     2.5 0.8 0.0 0.0 0.0  97 0.0 0.1 133   0  1K   0 dnx/95
 74241 root     2.5 0.8 0.0 0.0 0.0  97 0.0 0.1 128   5  1K   0 dnx/102
 74241 root     2.4 0.8 0.0 0.0 0.0  97 0.0 0.1 142   9  1K   0 dnx/85
 74241 root     2.4 0.8 0.0 0.0 0.0  97 0.0 0.1 143   3  1K   0 dnx/78
 74241 root     2.7 0.5 0.0 0.0 0.0  97 0.0 0.1 130   3  1K   0 dnx/113
 74241 root     2.6 0.6 0.0 0.0 0.0  97 0.0 0.1 140   3  1K   0 dnx/106
 74241 root     2.4 0.7 0.0 0.0 0.0  97 0.0 0.1 115   0  1K   0 dnx/110
 74241 root     2.3 0.8 0.0 0.0 0.0  97 0.0 0.1 136   6  1K   0 dnx/92
 74241 root     2.4 0.7 0.0 0.0 0.0  97 0.0 0.1 142   1  1K   0 dnx/107
 74241 root     2.5 0.6 0.0 0.0 0.0  97 0.0 0.1 129   3  1K   0 dnx/103
 74241 root     2.4 0.7 0.0 0.0 0.0  97 0.0 0.1 116   3  1K   0 dnx/84
 74241 root     2.4 0.7 0.0 0.0 0.0  97 0.0 0.1 129   4  1K   0 dnx/96
```
#### NEW
```
Total: 18 processes, 72 lwps, load averages: 2.04, 2.02, 2.47
   PID USERNAME USR SYS TRP TFL DFL LCK SLP LAT VCX ICX SCL SIG PROCESS/LWPID
 96751 root      36  40 0.1 0.0 0.0  23 0.0 1.6  1K 286 61K   0 dnx/31
 96751 root      22 1.0 0.1 0.0 0.0  76 0.4 0.9 489  97  7K   0 dnx/36
 96751 root      22 1.1 0.1 0.0 0.0  76 0.5 0.7 479  84  7K   0 dnx/54
 96751 root      21 1.0 0.1 0.0 0.0  77 0.0 0.9 479 108  7K   0 dnx/71
 96751 root      21 0.9 0.1 0.0 0.0  77 0.5 0.8 486  75  7K   1 dnx/34
 96751 root      21 1.0 0.1 0.0 0.0  77 0.0 1.4 482 177  7K   0 dnx/38
 96751 root      19 0.9 0.1 0.0 0.0  79 0.0 0.9 494  84  7K   0 dnx/63
 96751 root      19 0.9 0.1 0.0 0.0  79 0.0 0.8 483  82  7K   0 dnx/61
 96751 root      19 0.9 0.1 0.0 0.0  79 0.0 0.8 479  91  7K   1 dnx/50
 96751 root      19 0.9 0.0 0.0 0.0  80 0.0 0.5 495  30  7K   0 dnx/73
 96751 root      18 1.0 0.1 0.0 0.0  80 0.0 1.0 483 109  7K   0 dnx/66
 96751 root      18 0.9 0.1 0.0 0.0  80 0.0 0.7 478  85  7K   0 dnx/42
 96751 root      18 0.9 0.1 0.0 0.0  80 0.0 0.8 488  76  7K   0 dnx/65
 96751 root      18 1.0 0.0 0.0 0.0  80 0.0 0.5 484  56  7K   0 dnx/69
 96751 root      18 1.0 0.1 0.0 0.0  80 0.0 0.8 489 120  7K   1 dnx/51
```

* Using Brendan Gregg's FlameGraph [instructions](https://github.com/brendangregg/FlameGraph#1-capture-stacks) yields:
#### OLD
![dnx_stack_flamegraph](http://us-east.manta.joyent.com/shmeeny/public/dnx_user_flame.svg)
#### NEW
![dnx_stack_flamegraph](http://us-east.manta.joyent.com/shmeeny/public/dnx_improved_flame.svg)
