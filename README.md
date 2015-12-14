# CoreCLRWebAPISample
Sample CoreCLR WebAPI application used for trivial benchmarking

* CoreCLR performance on SmartOS LX branded Zones is currently sub-optimal. This
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
#### CoreCLR on LX Ubuntu 14.04
```
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
#### Node.js on LX Ubuntu 14.04
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

* Current points of suspicion:
  * [pal/thread.cpp](https://github.com/dotnet/coreclr/blob/a6bcc41247dc7fcf283219307e43dd12a43bf2d7/src/pal/src/thread/thread.cpp)
  * [pal/cs.pp](https://github.com/dotnet/coreclr/blob/a6bcc41247dc7fcf283219307e43dd12a43bf2d7/src/pal/src/sync/cs.cpp)


* Running [mytrace.d](https://gist.github.com/richardkiene/baaa15bbe7e5b8975045) yields:

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

* `prstat -mLc 1` yields:
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

* Using Brendan Gregg's FlameGraph [instructions](https://github.com/brendangregg/FlameGraph#1-capture-stacks) yields:

![dnx_stack_flamegraph](http://us-east.manta.joyent.com/shmeeny/public/dnx_user_flame.svg)
