namespace DietPhysics
{
    public class DietWorld
    {
        private const float Epsilon = 0.00001f;
        private List<ICollider> colliders_dynamic = new List<ICollider>();
        private List<ICollider> colliders_static = new List<ICollider>();
        private bool staticBaked = false;

        public void Bake()
        {
            staticBaked = true;
            // Statik collider'lar için spatial partitioning buraya eklenebilir.
        }

        #region ADD / REMOVE
        public void AddColliderDynamic(ICollider collider)
        {
            if (!colliders_dynamic.Contains(collider))
                colliders_dynamic.Add(collider);
        }

        public void AddColliderStatic(ICollider collider)
        {
            if (!colliders_static.Contains(collider))
            {
                colliders_static.Add(collider);
                staticBaked = false;
            }
        }

        public void RemoveColliderDynamic(ICollider collider)
        {
            colliders_dynamic.Remove(collider);
        }

        public void RemoveColliderStatic(ICollider collider)
        {
            colliders_static.Remove(collider);
            staticBaked = false;
        }
        #endregion

        // Tüm collider'ları (dynamic + static) birleştirerek döndürür.
        private IEnumerable<ICollider> AllColliders()
        {
            foreach (var c in colliders_dynamic) yield return c;
            foreach (var c in colliders_static) yield return c;
        }

        /// <summary>
        /// Eğer collider başka bir şeyle çakışıyorsa onu dışarı iter.
        /// Çakışma yoksa false döner. Çakışma varsa resolvedPos'u doldurur ve true döner.
        /// </summary>
        public bool ResolveOverlap(ICollider collider, out Vec3 resolvedPos)
        {
            resolvedPos = collider.GetPosition();
            bool anyOverlap = false;

            foreach (var other in AllColliders())
            {
                if (other == collider) continue;

                if (collider is DietSphere sphere)
                {
                    if (other is DietSphere otherSphere)
                    {
                        Vec3 delta = sphere.Position - otherSphere.Position;
                        float dist = delta.magnitude;
                        float minDist = sphere.Radius + otherSphere.Radius;
                        if (dist < minDist - Epsilon)
                        {
                            Vec3 pushDir = dist > Epsilon ? delta / dist : Vec3.up;
                            resolvedPos += pushDir * (minDist - dist + Epsilon);
                            sphere.Position = resolvedPos;
                            anyOverlap = true;
                        }
                    }
                    else if (other is DietBox box)
                    {
                        if (IsCollidingSphereToBox(sphere.Position, sphere.Radius, box, out Vec3 contact))
                        {
                            Vec3 normal = GetBoxNormalAtPoint(contact, box);
                            Vec3 toSurface = sphere.Position - contact;
                            float penetration = sphere.Radius - toSurface.magnitude;
                            if (penetration > 0)
                            {
                                resolvedPos += normal * (penetration + Epsilon);
                                sphere.Position = resolvedPos;
                                anyOverlap = true;
                            }
                        }
                    }
                    else if (other is DietCapsule capsule)
                    {
                        sphere.Position = resolvedPos;
                        if (IsCollidingSphereToCapsule(sphere, capsule, out Vec3 closestOnAxis, out Vec3 normal))
                        {
                            float totalRadius = sphere.Radius + capsule.Radius;
                            float dist = Vec3.Distance(sphere.Position, closestOnAxis);
                            float penetration = totalRadius - dist;
                            if (penetration > 0)
                            {
                                resolvedPos += normal * (penetration + Epsilon);
                                sphere.Position = resolvedPos;
                                anyOverlap = true;
                            }
                        }
                    }
                }
                else if (collider is DietCapsule cap)
                {
                    if (other is DietBox box)
                    {
                        cap.Position = resolvedPos;
                        if (IsCollidingCapsuleToBox_Simplified(cap, box, out Vec3 capClosest, out Vec3 boxClosest))
                        {
                            Vec3 sep = capClosest - boxClosest;
                            float dist = sep.magnitude;
                            float penetration = cap.Radius - dist;
                            if (penetration > 0)
                            {
                                Vec3 pushDir = dist > Epsilon ? sep / dist : GetBoxNormalAtPoint(boxClosest, box);
                                resolvedPos += pushDir * (penetration + Epsilon);
                                cap.Position = resolvedPos;
                                anyOverlap = true;
                            }
                        }
                    }
                    else if (other is DietSphere otherSphere)
                    {
                        cap.Position = resolvedPos;
                        if (IsCollidingSphereToCapsule(otherSphere, cap, out Vec3 closestOnAxis, out Vec3 normal))
                        {
                            float totalRadius = cap.Radius + otherSphere.Radius;
                            float dist = Vec3.Distance(otherSphere.Position, closestOnAxis);
                            float penetration = totalRadius - dist;
                            if (penetration > 0)
                            {
                                resolvedPos += normal * (penetration + Epsilon);
                                cap.Position = resolvedPos;
                                anyOverlap = true;
                            }
                        }
                    }
                    else if (other is DietCapsule otherCap)
                    {
                        cap.Position = resolvedPos;
                        if (IsCollidingCapsuleToCapsule(cap, otherCap, out _, out _, out Vec3 normal))
                        {
                            Vec3 closest = ClosestPointOnSegmentToPoint(cap.SegmentStart, cap.SegmentEnd,
                                                                         otherCap.Position);
                            float dist = Vec3.Distance(closest, otherCap.Position);
                            float penetration = cap.Radius + otherCap.Radius - dist;
                            if (penetration > 0)
                            {
                                resolvedPos += normal * (penetration + Epsilon);
                                cap.Position = resolvedPos;
                                anyOverlap = true;
                            }
                        }
                    }
                }
                // DietBox depenetration buraya eklenebilir.
            }

            return anyOverlap;
        }

        public bool SweepTest(ICollider collider, Vec3 direction, float distance, int iterations, out Vec3 collidedPosition)
        {
            collidedPosition = Vec3.zero;

            if (distance <= Epsilon)
                return false;

            if (iterations < 1)
                iterations = 1;

            Vec3 firstPos = collider.GetPosition();
            Vec3 currentPos = firstPos;

            float stepSize = distance / iterations;

            for (int i = 0; i <= iterations; i++)
            {
                foreach (var other in AllColliders())
                {
                    if (other == collider) continue;

                    bool hit = false;

                    if (collider is DietSphere sphere)
                    {
                        hit = SweepSphere(sphere, currentPos, other, out collidedPosition);
                    }
                    else if (collider is DietBox box)
                    {
                        hit = SweepBox(box, currentPos, other, out collidedPosition);
                    }
                    else if (collider is DietCapsule capsule)
                    {
                        hit = SweepCapsule(capsule, currentPos, other, out collidedPosition);
                    }

                    if (hit)
                    {
                        // Collider pozisyonunu her zaman orijinal konuma döndür.
                        RestorePosition(collider, firstPos);
                        return true;
                    }
                }

                currentPos += direction * stepSize;
            }

            RestorePosition(collider, firstPos);
            return false;
        }

        private void RestorePosition(ICollider collider, Vec3 pos)
        {
            if (collider is DietSphere s) s.Position = pos;
            else if (collider is DietBox b) b.Position = pos;
            else if (collider is DietCapsule c) c.Position = pos;
        }

        // ─── SPHERE SWEEP ─────────────────────────────────────────────────────────
        private bool SweepSphere(DietSphere sphere, Vec3 currentPos, ICollider other, out Vec3 collidedPosition)
        {
            collidedPosition = Vec3.zero;

            if (other is DietBox box)
            {
                if (IsCollidingSphereToBox(currentPos, sphere.Radius, box, out Vec3 contactPoint))
                {
                    Vec3 normal = GetBoxNormalAtPoint(contactPoint, box);
                    collidedPosition = contactPoint + normal * sphere.Radius;
                    return true;
                }
            }
            else if (other is DietSphere otherSphere)
            {
                if (IsCollidingSphereToSphere(currentPos, sphere.Radius, otherSphere))
                {
                    Vec3 dir = (currentPos - otherSphere.GetPosition()).normalized;
                    collidedPosition = otherSphere.GetPosition() + dir * (otherSphere.Radius + sphere.Radius);
                    return true;
                }
            }
            else if (other is DietCapsule capsule)
            {
                sphere.Position = currentPos;
                if (IsCollidingSphereToCapsule(sphere, capsule, out Vec3 closestOnAxis, out Vec3 normal))
                {
                    float totalRadius = sphere.Radius + capsule.Radius;
                    collidedPosition = closestOnAxis + normal * totalRadius;
                    return true;
                }
            }

            return false;
        }

        // ─── BOX SWEEP ────────────────────────────────────────────────────────────
        private bool SweepBox(DietBox box, Vec3 currentPos, ICollider other, out Vec3 collidedPosition)
        {
            collidedPosition = Vec3.zero;
            box.Position = currentPos;

            if (other is DietBox otherBox)
            {
                if (IsCollidingBoxToBox(box, otherBox, out Vec3 normal, out float depth))
                {
                    collidedPosition = currentPos + normal * depth;
                    return true;
                }
            }
            else if (other is DietSphere sphere)
            {
                // Box-Sphere: Sphere tarafından test ederiz (simetrik).
                if (IsCollidingSphereToBox(sphere.GetPosition(), sphere.Radius, box, out Vec3 contactPoint))
                {
                    Vec3 normal = GetBoxNormalAtPoint(contactPoint, box);
                    collidedPosition = currentPos - normal * sphere.Radius;
                    return true;
                }
            }
            else if (other is DietCapsule capsule)
            {
                if (IsCollidingCapsuleToBox_Simplified(capsule, box, out Vec3 capsuleClosest, out Vec3 boxClosest))
                {
                    Vec3 sep = capsuleClosest - boxClosest;
                    float dist = sep.magnitude;
                    Vec3 moveDir = dist > 0.001f ? sep / dist : GetBoxNormalAtPoint(boxClosest, box);
                    float moveAmount = capsule.Radius - dist + Epsilon;
                    collidedPosition = currentPos - moveDir * moveAmount;
                    return true;
                }
            }

            return false;
        }

        // ─── CAPSULE SWEEP ────────────────────────────────────────────────────────
        private bool SweepCapsule(DietCapsule capsule, Vec3 currentPos, ICollider other, out Vec3 collidedPosition)
        {
            collidedPosition = Vec3.zero;
            capsule.Position = currentPos;

            if (other is DietBox box)
            {
                if (IsCollidingCapsuleToBox_Simplified(capsule, box, out Vec3 capsuleAxisClosest, out Vec3 boxClosest))
                {
                    Vec3 sep = capsuleAxisClosest - boxClosest;
                    float dist = sep.magnitude;
                    Vec3 moveDir = dist > 0.001f ? sep / dist : GetBoxNormalAtPoint(boxClosest, box);
                    float moveAmount = capsule.Radius - dist + Epsilon;
                    collidedPosition = currentPos + moveDir * moveAmount;
                    return true;
                }
            }
            else if (other is DietSphere sphere)
            {
                if (IsCollidingSphereToCapsule(sphere, capsule, out Vec3 closestOnAxis, out Vec3 normal))
                {
                    float totalRadius = capsule.Radius + sphere.Radius;
                    Vec3 resolvedAxisPoint = sphere.GetPosition() - normal * totalRadius;
                    Vec3 offset = currentPos - closestOnAxis;
                    collidedPosition = resolvedAxisPoint + offset;
                    return true;
                }
            }
            else if (other is DietCapsule otherCapsule)
            {
                if (IsCollidingCapsuleToCapsule(capsule, otherCapsule, out Vec3 closestA, out Vec3 closestB, out Vec3 normal))
                {
                    float dist = (closestA - closestB).magnitude;
                    float targetDist = capsule.Radius + otherCapsule.Radius;
                    float penetration = targetDist - dist;
                    collidedPosition = penetration > 0 ? currentPos + normal * penetration : currentPos;
                    return true;
                }
            }

            return false;
        }

        // ─── BOX TO BOX (SAT – eksenlere göre) ───────────────────────────────────
        private bool IsCollidingBoxToBox(DietBox a, DietBox b, out Vec3 normal, out float depth)
        {
            normal = Vec3.up;
            depth = float.MaxValue;

            Quat rotA = Quat.Euler(a.Rotation);
            Quat rotB = Quat.Euler(b.Rotation);

            Vec3[] axesA = { rotA * Vec3.right, rotA * Vec3.up, rotA * Vec3.forward };
            Vec3[] axesB = { rotB * Vec3.right, rotB * Vec3.up, rotB * Vec3.forward };

            Vec3[] testAxes = new Vec3[15];
            for (int i = 0; i < 3; i++) testAxes[i] = axesA[i];
            for (int i = 0; i < 3; i++) testAxes[3 + i] = axesB[i];
            int idx = 6;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    Vec3 cross = Vec3.Cross(axesA[i], axesB[j]);
                    testAxes[idx++] = cross.sqrMagnitude > Epsilon ? cross.normalized : Vec3.zero;
                }

            Vec3 translation = b.Position - a.Position;

            foreach (var axis in testAxes)
            {
                if (axis.sqrMagnitude < Epsilon) continue;

                float projA = ProjectBox(a, axis);
                float projB = ProjectBox(b, axis);
                float projT = Mtf.Abs(Vec3.Dot(translation, axis));

                float overlap = projA + projB - projT;
                if (overlap <= 0) return false; // Separating axis found.

                if (overlap < depth)
                {
                    depth = overlap;
                    normal = Vec3.Dot(translation, axis) < 0 ? -axis : axis;
                }
            }

            return true;
        }

        private float ProjectBox(DietBox box, Vec3 axis)
        {
            Quat rot = Quat.Euler(box.Rotation);
            Vec3 h = box.Size * 0.5f;
            return h.x * Mtf.Abs(Vec3.Dot(rot * Vec3.right, axis))
                 + h.y * Mtf.Abs(Vec3.Dot(rot * Vec3.up, axis))
                 + h.z * Mtf.Abs(Vec3.Dot(rot * Vec3.forward, axis));
        }

        // ─── SPHERE TO BOX ────────────────────────────────────────────────────────
        private bool IsCollidingSphereToBox(Vec3 spherePos, float radius, DietBox box, out Vec3 contactPoint)
        {
            Quat rotation = Quat.Euler(box.Rotation);
            Vec3 halfSize = box.Size * 0.5f;

            // Center bir yerel offset — gerçek dünya merkezi: Position + Center
            Vec3 worldCenter = box.Position + rotation * box.Center;
            Vec3 localSpherePos = Quat.Inverse(rotation) * (spherePos - worldCenter);

            bool inside =
                localSpherePos.x >= -halfSize.x && localSpherePos.x <= halfSize.x &&
                localSpherePos.y >= -halfSize.y && localSpherePos.y <= halfSize.y &&
                localSpherePos.z >= -halfSize.z && localSpherePos.z <= halfSize.z;

            Vec3 localClosest = new Vec3(
                Mtf.Clamp(localSpherePos.x, -halfSize.x, halfSize.x),
                Mtf.Clamp(localSpherePos.y, -halfSize.y, halfSize.y),
                Mtf.Clamp(localSpherePos.z, -halfSize.z, halfSize.z)
            );

            if (inside)
            {
                float dx = halfSize.x - Mtf.Abs(localSpherePos.x);
                float dy = halfSize.y - Mtf.Abs(localSpherePos.y);
                float dz = halfSize.z - Mtf.Abs(localSpherePos.z);

                if (dx <= dy && dx <= dz)
                    localClosest.x = localSpherePos.x < 0 ? -halfSize.x : halfSize.x;
                else if (dy <= dz)
                    localClosest.y = localSpherePos.y < 0 ? -halfSize.y : halfSize.y;
                else
                    localClosest.z = localSpherePos.z < 0 ? -halfSize.z : halfSize.z;
            }

            Vec3 worldClosest = rotation * localClosest + worldCenter;
            Vec3 toCenter = spherePos - worldClosest;
            float mag = toCenter.magnitude;

            if (inside || mag <= radius)
            {
                contactPoint = worldClosest;
                return true;
            }

            contactPoint = Vec3.zero;
            return false;
        }

        // ─── SPHERE TO SPHERE ─────────────────────────────────────────────────────
        private bool IsCollidingSphereToSphere(Vec3 position, float radius, DietSphere otherSphere)
        {
            float distance = Vec3.Distance(position, otherSphere.Position);
            return distance <= (otherSphere.Radius + radius);
        }

        // ─── SPHERE TO CAPSULE ────────────────────────────────────────────────────
        private bool IsCollidingSphereToCapsule(DietSphere sphere, DietCapsule capsule, out Vec3 closestPointOnAxis, out Vec3 normal)
        {
            closestPointOnAxis = Vec3.zero;
            normal = Vec3.zero;

            Vec3 capsuleAxis = capsule.Axis;
            float axisLengthSqr = capsuleAxis.sqrMagnitude;

            if (axisLengthSqr < 0.0001f)
                return IsCollidingPointToSphere(capsule.SegmentStart, sphere, capsule.Radius, out closestPointOnAxis, out normal);

            Vec3 sphereCenter = sphere.GetPosition();
            Vec3 pointToStart = sphereCenter - capsule.SegmentStart;

            float projection = Mtf.Clamp01(Vec3.Dot(pointToStart, capsuleAxis) / axisLengthSqr);
            closestPointOnAxis = capsule.SegmentStart + capsuleAxis * projection;

            Vec3 distVec = sphereCenter - closestPointOnAxis;
            float distSqr = distVec.sqrMagnitude;
            float totalRadius = sphere.Radius + capsule.Radius;

            if (distSqr < totalRadius * totalRadius)
            {
                float dist = Mtf.Sqrt(distSqr);
                if (dist > 0.0001f)
                {
                    normal = distVec / dist;
                }
                else
                {
                    normal = Vec3.Cross(capsuleAxis, Vec3.up).normalized;
                    if (normal.sqrMagnitude < 0.0001f)
                        normal = Vec3.Cross(capsuleAxis, Vec3.right).normalized;
                    if (normal.sqrMagnitude < 0.0001f)
                        normal = Vec3.up;
                }
                return true;
            }
            return false;
        }

        // ─── POINT TO SPHERE ──────────────────────────────────────────────────────
        private bool IsCollidingPointToSphere(Vec3 point, DietSphere sphere, float pointRadius, out Vec3 closestPoint, out Vec3 normal)
        {
            closestPoint = point;
            normal = Vec3.zero;

            Vec3 sphereCenter = sphere.GetPosition();
            Vec3 distVec = sphereCenter - point;
            float distSqr = distVec.sqrMagnitude;
            float totalRadius = sphere.Radius + pointRadius;

            if (distSqr < totalRadius * totalRadius)
            {
                float dist = Mtf.Sqrt(distSqr);
                normal = dist > 0.0001f ? distVec / dist : Vec3.up;
                return true;
            }
            return false;
        }

        // ─── CAPSULE TO BOX ───────────────────────────────────────────────────────
        private bool IsCollidingCapsuleToBox_Simplified(DietCapsule capsule, DietBox box,
            out Vec3 capsuleAxisClosestPoint, out Vec3 boxClosestPoint)
        {
            capsuleAxisClosestPoint = Vec3.zero;
            boxClosestPoint = Vec3.zero;

            Vec3 closestOnSegment = ClosestPointOnSegmentToPoint(capsule.SegmentStart, capsule.SegmentEnd, box.Position);

            Quat rotation = Quat.Euler(box.Rotation);
            Quat invRotation = Quat.Inverse(rotation);
            Vec3 halfSize = box.Size * 0.5f;

            Vec3 localOnSegment = invRotation * (closestOnSegment - box.Position);

            Vec3 localOnBox = new Vec3(
                Mtf.Clamp(localOnSegment.x, -halfSize.x, halfSize.x),
                Mtf.Clamp(localOnSegment.y, -halfSize.y, halfSize.y),
                Mtf.Clamp(localOnSegment.z, -halfSize.z, halfSize.z)
            );

            Vec3 worldOnBox = rotation * localOnBox + box.Position;

            capsuleAxisClosestPoint = closestOnSegment;
            boxClosestPoint = worldOnBox;

            Vec3 diff = closestOnSegment - worldOnBox;
            return diff.sqrMagnitude <= capsule.Radius * capsule.Radius + Epsilon;
        }

        // ─── CAPSULE TO CAPSULE ───────────────────────────────────────────────────
        private bool IsCollidingCapsuleToCapsule(DietCapsule capsuleA, DietCapsule capsuleB,
            out Vec3 closestPointA, out Vec3 closestPointB, out Vec3 normal)
        {
            closestPointA = Vec3.zero;
            closestPointB = Vec3.zero;
            normal = Vec3.up;

            Vec3 p0 = capsuleA.SegmentStart;
            Vec3 p1 = capsuleA.SegmentEnd;
            Vec3 u = p1 - p0;
            float lenA = u.magnitude;

            Vec3 q0 = capsuleB.SegmentStart;
            Vec3 q1 = capsuleB.SegmentEnd;
            Vec3 v = q1 - q0;
            float lenB = v.magnitude;

            float totalRadius = capsuleA.Radius + capsuleB.Radius;
            float totalRadiusSq = totalRadius * totalRadius;

            // Her iki kapsül de nokta ise → sphere-sphere testi.
            if (lenA < Epsilon && lenB < Epsilon)
            {
                Vec3 between = p0 - q0;
                float distSqr = between.sqrMagnitude;
                if (distSqr < totalRadiusSq - Epsilon)
                {
                    closestPointA = p0;
                    closestPointB = q0;
                    normal = distSqr > Epsilon ? between.normalized : Vec3.up;
                    return true;
                }
                return false;
            }

            // Sadece A nokta ise → sphere-capsule.
            if (lenA < Epsilon)
            {
                Vec3 closest = ClosestPointOnSegmentToPoint(q0, q1, p0);
                Vec3 diff = p0 - closest;
                float distSqr = diff.sqrMagnitude;
                if (distSqr < totalRadiusSq - Epsilon)
                {
                    closestPointA = p0;
                    closestPointB = closest;
                    normal = distSqr > Epsilon ? diff.normalized : Vec3.up;
                    return true;
                }
                return false;
            }

            // Sadece B nokta ise → sphere-capsule (ters yön).
            if (lenB < Epsilon)
            {
                Vec3 closest = ClosestPointOnSegmentToPoint(p0, p1, q0);
                Vec3 diff = closest - q0;
                float distSqr = diff.sqrMagnitude;
                if (distSqr < totalRadiusSq - Epsilon)
                {
                    closestPointA = closest;
                    closestPointB = q0;
                    normal = distSqr > Epsilon ? diff.normalized : Vec3.up;
                    return true;
                }
                return false;
            }

            Vec3 w0 = p0 - q0;
            float a = Vec3.Dot(u, u);
            float b = Vec3.Dot(u, v);
            float c = Vec3.Dot(v, v);
            float d = Vec3.Dot(u, w0);
            float e = Vec3.Dot(v, w0);
            float det = a * c - b * b;

            float s, t;

            if (det < Epsilon) // Paralel segmentler.
            {
                // B'nin Q0'ını A eksenine yansıt.
                float s_q0 = Mtf.Clamp(-d / a, 0f, 1f);
                float s_q1 = Mtf.Clamp((-d + b) / a, 0f, 1f);

                // Dik mesafeyi kontrol et.
                Vec3 perpVec = w0 - (Vec3.Dot(w0, u) / a) * u;
                if (perpVec.sqrMagnitude >= totalRadiusSq - Epsilon) return false;

                float s_mid = (s_q0 + s_q1) * 0.5f;
                closestPointA = p0 + s_mid * u;

                float t_mid = Mtf.Clamp01(Vec3.Dot(closestPointA - q0, v) / (lenB * lenB));
                closestPointB = q0 + t_mid * v;

                Vec3 diff = closestPointA - closestPointB;
                float distSqr = diff.sqrMagnitude;
                if (distSqr < totalRadiusSq - Epsilon)
                {
                    normal = distSqr > Epsilon ? diff.normalized
                           : (perpVec.sqrMagnitude > Epsilon ? perpVec.normalized : Vec3.up);
                    return true;
                }
                return false;
            }

            s = Mtf.Clamp01((b * e - c * d) / det);
            t = Mtf.Clamp01((a * e - b * d) / det);

            closestPointA = p0 + s * u;
            closestPointB = q0 + t * v;

            Vec3 distVec = closestPointA - closestPointB;
            float distanceSqr = distVec.sqrMagnitude;

            if (distanceSqr < totalRadiusSq - Epsilon)
            {
                if (distanceSqr > Epsilon)
                {
                    normal = distVec.normalized;
                }
                else
                {
                    // Tam üst üste binme: cross product ile normal bul.
                    normal = Vec3.Cross(u, v).normalized;
                    if (normal.sqrMagnitude < Epsilon)
                        normal = GetPerpendicular(u);
                    if (Vec3.Dot(normal, w0) < 0) normal = -normal;
                }
                return true;
            }
            return false;
        }

        // ─── YARDIMCI METODLAR ────────────────────────────────────────────────────
        private Vec3 ClosestPointOnSegmentToPoint(Vec3 segStart, Vec3 segEnd, Vec3 point)
        {
            Vec3 segDir = segEnd - segStart;
            float segLenSqr = segDir.sqrMagnitude;
            if (segLenSqr < Epsilon) return segStart;
            float t = Mtf.Clamp01(Vec3.Dot(point - segStart, segDir) / segLenSqr);
            return segStart + t * segDir;
        }

        private Vec3 GetBoxNormalAtPoint(Vec3 contactPoint, DietBox box)
        {
            Quat rotation = Quat.Euler(box.Rotation);
            // Center bir yerel offset — gerçek dünya merkezi: Position + Center
            Vec3 worldCenter = box.Position + rotation * box.Center;
            Vec3 localContact = Quat.Inverse(rotation) * (contactPoint - worldCenter);
            Vec3 halfSize = box.Size * 0.5f;

            float dx = halfSize.x - Mtf.Abs(localContact.x);
            float dy = halfSize.y - Mtf.Abs(localContact.y);
            float dz = halfSize.z - Mtf.Abs(localContact.z);

            Vec3 localNormal;
            if (dx < dy && dx < dz)
                localNormal = new Vec3(localContact.x < 0 ? -1 : 1, 0, 0);
            else if (dy < dz)
                localNormal = new Vec3(0, localContact.y < 0 ? -1 : 1, 0);
            else
                localNormal = new Vec3(0, 0, localContact.z < 0 ? -1 : 1);

            return (rotation * localNormal).normalized;
        }

        /// <summary>
        /// Verilen vektöre dik, normalize edilmiş bir vektör döndürür.
        /// Sıfır bölme hatası içermez.
        /// </summary>
        private Vec3 GetPerpendicular(Vec3 vec)
        {
            // En küçük bileşeni sıfırlayarak güvenli bir dik vektör üret.
            float ax = Mtf.Abs(vec.x);
            float ay = Mtf.Abs(vec.y);
            float az = Mtf.Abs(vec.z);

            Vec3 other;
            if (ax <= ay && ax <= az)
                other = Vec3.right;
            else if (ay <= az)
                other = Vec3.up;
            else
                other = Vec3.forward;

            return Vec3.Cross(vec, other).normalized;
        }
    }
}
