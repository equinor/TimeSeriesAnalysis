# The UnitModel 

> [!Note]
> As we shall se, the ``UnitModel`` has five very useful proprieties:
> 1. the number of parameters is as low as possible, given the model's capability, **reducing the likelihood of over-fitting**,
> 2. the parameters can be given an intuitive physical interpretation (making the model **"grey-box"** rather than "black box"),
> 3. the model is **linear-in-parameters**, meaning parameters can be found using linear regression,
> 4. the model can **describe curvatures**, a nonlinearity in the process gain, and
> 5. by turning on or off different parameters, the model can take different capabilities, it acts as a **flexible "model set"**.

## Details

The "default" model in this library is ``UnitModel``, intended to be a model that can describe most average process systems. 
It aims to *avoid over-parametrization*, as this is known to cause issues on real-world datasets with limited excitation.

As a design choice, the model has built in support for *explicitly treating* **measured/modeled external disturbances**, for use in analysis of closed-loop systems.

The model is on the form
$$
y[k] = x[k] + d[k]
$$
where $x[k]$ is the *state* at time $k$, $d[k]$ is an *external disturbance* at time $k$,

furthermore the *state* $x[k]$ is modeled as
$$
x[k] = a \cdot x[k-1] + b \cdot (u[k-td]-u_0) + q					
$$
where $t_d$ here denotes the time-delay in samples.

The parameter $a$ in the above equation should always be between $[0;0.9999]$ .

> [!Note]
> Notice that if $a=0$ the the recursive term is stricken, the disturbance is neglected and time delay is zero, then the model reverts to standard linear static model $y[k] = b \cdot u[k]$.


The model 
- is a *difference model* as $x[k]$ depends on the previous value $x[k-1]$.
- is *local* as the terms $b$ and optionally $c$ apply locally around the operating point $u_0$. 

The above shows the model for a single input $u$, but the model excepts any number of inputs, so in the case of two inputs $u_1$ and $u_2$ for instance
$$
x[k] = a \cdot x[k-1] + b_1\cdot(u_1[k-t_d]-u_{10})+ b_2\cdot(u_2[k-t_d]-u_{20}) + q		
$$

> [!Note]
> All inputs in the same model by design share the same dynamic parameters $a$ and $t_d$. If multiple inputs act on a single output $y with different
> dynamics, then this should be modeled by two or more separate "default models" that are then added together - in identification the input of other 
> sub-models can be accounted for by the disturbance-term $d$

## Second-order polynomial nonlinear gain

Optionally the default model can be extended with a square term:
$$ 
x[k] = a \cdot x[k-1] + b \cdot(u[k-t_d]-u_0) + c/u_{Norm} \cdot(u[k-t_d]-u_0)^2 + q	
$$
Internally the parameter $c$ is referred to as the **"Curvature"** of the default model.

$u_{Norm}$ is a scaling parameter that is intended to ensure that the parameters $b$ and $c$
are of approximately equal scale during identification. 
It is recommended to choose $u_{Norm}$ equal to how much $u$ is expect to vary from $u_0$.
For example, if $u_0$ and $u$ is expected to vary in the range $[20,80]$, then $u_{Norm}$
should be chosen as ``30``.

>[!Note]
> The sign of the curvature terms $c$ can be *either positive or negative*. 
> If $c$ is **negative**, then this means that the gains is **higher below u0** and **lower above u0**.
> If $c$ is **positive**, then this means that the gains is **lower below u0** and **higher above u0**.

>[!Note]
> If the model has curvature terms $c$ then the process gain depends on **both** parameters
> $b$ and $c$.
    

##  Process gain and time constant
 
$a $ is related to the *time-constant* $T_c$ by the equation
$$
a = \frac{1}{1+\frac{Ts}{Tc}}
$$  
where $T_s$ is the *sampling time*, so that:
$$
Tc = \frac{Ts}{\frac{1}{a}-1}.
$$  

> [!Note]
>The time-constant is far easier to interpret intuitively, and is much easier to relate to PID-controller terms or filters than $a$ directly.

The *process gain* $G_1$ between an input $u_1$ and the output $y$ is: 
$$
G_1 = \frac{b_1}{1-a}.
$$
If the model has two inputs, the second input $u_2$ will have a process gain that depends on $b_2$
$$
G_2 = \frac{b_2}{1-a}.
$$

If the default model has a *second-order* term with parameters $c1,c2,...$,the the process gains are no longer constant, but will vary dependent on the value of the inputs $u$.
Thus, these models are *"nonlinear in u"*, but the beauty of this model is that these model are still *"linear in parameters $[a, b_1,..b_N, c_1,...C_N]$*  so that the identifier can be 
based on linear regression.

The process gains become functions of $u$, so for instance for $u_1$, the process gain will be $G_1(u_1)$ becomes:
$$
G_1(u_1) = \frac{b_1}{1-a} + \frac{c_1}{1-a}\cdot 2 \cdot u_1.
$$







