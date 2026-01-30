
## The unit-model : second order systems



>[!Note]
> Currently, the ``UnitModel`` has support for manually specifying a second-order term $\zeta$
> but neither the ``UnitIdentifier`` or ``ClosedLoopUnitIdentifier`` support determining this parameter 
> automatically. Using the below equations, it should be possible to extend these solvers to 
> attempt to fit a second-order model that is linear-in-parameters, and then relate the parameters found
> to more interpretable parameters like time-constant and damping ratio $\zeta$

A simple second order system is often described in the s-plane as

$$

\frac{y(s)}{u(s)} = \frac{\omega_n^2}{s^2+2\cdot\zeta\cdot\omega_n\cdot s + \omega_n^2 }

$$


where 
$\omega_n$ ("omega") is the natural frequency, and 
$\zeta$ ("zeta") is the damping ratio.
Generally the damping ratio describes the overshoot of the system, lower damping ratios correspond to higher overshoot.

Ideally, the unit model should have the ability to describe the above second order systems, but the extension should preferably 
be analogous to the above first-order system treatment.

Fitting a second order system will require adding a second parameter for the dynamics, so parameters 
$(a_1,a_2)$

would need to be fitted to data in the below difference equation

$$
y[k] = a_1 \cdot y[k-1] + a_2 \cdot y[k-2] + b \cdot u[k] + q
$$

and these would need to converted into more easily relatable parameters after identification.

$$
y(s)\cdot \left(s^2+2\cdot\zeta\cdot\omega_n\cdot s + \omega_n^2  \right) = \omega_n^2 \cdot u(s) 
$$

and converting to the time-domain
$$
\frac{d^2y(t)}{dt^2} +  \left(2\cdot\zeta\cdot\omega_n \right) \cdot \frac{dy(t)}{dt} + \omega_n^2 \cdot y(t) = \omega_n^2 \cdot u(t) 
$$

Now the above equation must be approximated in a difference form:

$$
\frac{dy[t]}{dt} \approx \frac{y[k]-y[k-1]}{T_s}
$$
 
and 

$$
\frac{d^2y[t]}{dt^2} \approx \frac{y[k]-y[k-1]}{T_s} - \frac{y[k-1]-y[k-2]}{T_s} = \frac{y[k] - 2\cdot y[k-1] + y[k-2]}{T_s}
$$

so that:

$$
\frac{y[k] - 2\cdot y[k-1] + y[k-2]}{T_s} +  \left(2\cdot\zeta\cdot\omega_n \right) \cdot \frac{y[k]-y[k-1]}{T_s} + \omega_n^2 \cdot y(k) = \omega_n^2 \cdot u(k) 
$$

$$
y[k] \left (1/T_s + \frac{2\cdot\zeta\cdot\omega_n}{T_s}+\omega_n^2 \right)  =   \frac{2\cdot y[k-1] - y[k-2]}{T_s} +  \left(2\cdot\zeta\cdot\omega_n \right) \cdot \frac{y[k-1]}{T_s} +  \omega_n^2 \cdot u(k) 
$$

$$
y[k] \left (1/T_s + \frac{2\cdot\zeta\cdot\omega_n}{T_s}+\omega_n^2 \right)  =   \frac{2 + 2\cdot\zeta\cdot\omega_n }{T_s} \cdot y[k-1] -  \frac{-1}{T_s}\cdot y[k-2] +  \omega_n^2 \cdot u(k) 
$$

thus

$$
a_1 = \frac{2 + 2\cdot\zeta\cdot\omega_n }{T_s} \cdot \left(  1/T_s + \frac{2\cdot\zeta\cdot\omega_n}{T_s}+\omega_n^2 \right)^{-1}
$$

$$
a_2 =  -\frac{1}{T_s} \cdot \left(  1/T_s + \frac{2\cdot\zeta\cdot\omega_n}{T_s}+\omega_n^2 \right)^{-1}
$$

$$
b =  \omega_n^2 \cdot \left(  1/T_s + \frac{2\cdot\zeta\cdot\omega_n}{T_s}+\omega_n^2 \right)^{-1}
$$

#### Steady-state
The equation 

$$
y[k] = a_1 \cdot y[k-1] + a_2 \cdot y[k-2] + b \cdot u[k] + q
$$

is in steady-state when 

$y[k] =  y[k-1] = y[k-2] $

in which case:

$$
y[k] = a_1 \cdot y[k] + a_2 \cdot y[k] + b \cdot u[k] + q
$$

$$
y[k] \cdot(1-a_1-a_2) = b \cdot u[k] + q
$$

So the steady-state gain from depends on 

$$
y[k] = \frac{b}{1-a_1-a_2} \cdot u[k] + \frac{q}{1-a_1-a_2} 
$$

*Note:($1-a_1-a_2$) can be further simplified*

---

#### Second order filter

When simulating, it is useful to express the second order dynamics as steady-state dynamics that is fed through a filter.

In the above equations, if 
$u[k]=y_{ss}(u[k])$
 (the steady-state output for input $u[k]$)

Since $y[k] = y_{ss}$ in the steady state
$b = 1-a_1-a_2 , q = 0$

The second order filter of gain 1 is thus:

$$y[k] = a_1 \cdot y[k-1] + a_2 \cdot y[k-2] + (1-a_1-a_2) \cdot u[k]$$

#### Interpreting the damping ratio
How to interpret the damping ratio in practical terms

- $\zeta >= 1$
 means no overshoot, i.e. in that case the system is better described by a first-order system. 
- $\ 0.3 < \zeta < 1$
 will mean a single overshoot peak, and a single dip after the initial overshoot
- as $\zeta <0.3$ gets smaller (approaches zero) there is more rapid overshoot and more visible oscillations after the overshoot in a  step response.

