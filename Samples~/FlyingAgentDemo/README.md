# Flying Agent Demo

Open `FlyingAgentDemo.unity` and press Play. Geometry is created at runtime.
The cyan cube alternates between green and yellow endpoints, routing around a central
block. No keyboard controls or custom project settings are required.

To inspect paths: select Flying Agent during Play, enable Debug Mode and Show Path,
and enable Gizmos in Scene view. Select Navigation Volume and enable Debug Mode and
Show Free Octants to inspect cells. Avoid leaving every octant gizmo enabled on large volumes.

If the agent stops, check Console messages, collision masks and radius. The sample
uses Default-layer static obstacles and excludes non-colliding visual markers. For
player builds retain the selected Unlit shader in Graphics Settings; this sample is
primarily designed for Editor Play Mode. See the package validation checklist.
