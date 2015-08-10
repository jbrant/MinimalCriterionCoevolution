using System;

namespace SharpNeat.Domains.MazeNavigation.Components
{
    public class Circle2D
    {
        public Point2D p;
        public double radius;

        public Circle2D()
        {
        }

        public Circle2D(Circle2D other)
        {
            p = other.p;
            radius = other.radius;
        }

        public Circle2D(Point2D a, double rad)
        {
            radius = rad;
            p = a;
        }

        //test if circle collides with other circle
        //used in robot collision detection
        public bool collide(Circle2D other)
        {
            var dx = other.p.x - p.x;
            var dy = other.p.y - p.y;
            dx *= dx;
            dy *= dy;
            var rad_sum = other.radius + radius;
            rad_sum *= rad_sum;
            if ((dx + dy) < rad_sum)
                return true;
            return false;
        }
    }

    //point class, does some basic trig
    public class Point2D
    {
        public double x, y;

        public Point2D()
        {
        }

        public Point2D(double a, double b)
        {
            x = a;
            y = b;
        }

        public Point2D(Point2D another)
        {
            x = another.x;
            y = another.y;
        }

        //calculate angle of line from origin to this point
        public double angle()
        {
            if (x == 0.0)
            {
                if (y > 0)
                    return 3.14/2.0;
                return 3.14*3.0/2.0;
            }
            var ang = Math.Atan(y/x);
            if (x > 0)
                return ang;

            return ang + 3.14;
        }

        //rotate this point about another point
        public void rotate(double angle, Point2D point)
        {
            x -= point.x;
            y -= point.y;

            var ox = x;
            var oy = y;

            x = Math.Cos(angle)*ox - Math.Sin(angle)*oy;
            y = Math.Sin(angle)*ox + Math.Cos(angle)*oy;

            x += point.x;
            y += point.y;
        }

        //what is the squared dist to another point
        public double distance_sq(Point2D point)
        {
            var dx = point.x - x;
            var dy = point.y - y;
            return dx*dx + dy*dy;
        }

        public double manhattenDistance(Point2D point)
        {
            var dx = Math.Abs(point.x - x);
            var dy = Math.Abs(point.y - y);
            return dx + dy;
        }

        //what is the distance to another point
        public double distance(Point2D point)
        {
            return Math.Sqrt(distance_sq(point));
        }
    }

    //line segment class, does some basic trig
    public class Line2D
    {
        public Point2D p1, p2;

        public Line2D()
        {
        }

        public Line2D(Point2D a, Point2D b)
        {
            p1 = a;
            p2 = b;
        }

        public Line2D(Line2D other)
        {
            p1 = other.p1;
            p2 = other.p2;
        }

        public void scale(double factor)
        {
            var mid = midpoint();
            p1.x = mid.x + (p1.x - mid.x)*factor;
            p1.y = mid.y + (p1.y - mid.y)*factor;
            p2.x = mid.x + (p2.x - mid.x)*factor;
            p2.y = mid.y + (p2.y - mid.y)*factor;
        }

        //what is the midpoint of this line
        public Point2D midpoint()
        {
            var x = (p1.x + p2.x)/2.0;
            var y = (p1.y + p2.y)/2.0;
            return new Point2D(x, y);
        }

        //calculate the nearest intersection of this line
        //with a circle -- if the line is interpreted as a ray
        //going from its first endpoint to the second
        public double nearest_intersection(Circle2D C, out bool found)
        {
            double dx, dy;

            dx = p2.x - p1.x;
            dy = p2.y - p1.y;

            var px = p1.x - C.p.x;
            var py = p1.y - C.p.y;

            var a = dx*dx + dy*dy;
            var b = 2*px*dx + 2*py*dy;
            var c = px*px + py*py - C.radius*C.radius;

            var det = b*b - 4.0*a*c;

            if (det < 0.0)
            {
                found = false;
                return -1.0;
            }

            var sqrt_det = Math.Sqrt(det);
            var t1 = (-b + sqrt_det)/(2*a);
            var t2 = (-b - sqrt_det)/(2*a);

            found = false;
            var t = 0.0;
            if (t2 < 0)
            {
                if (t1 > 0)
                {
                    found = true;
                    t = t1;
                }
            }
            else
            {
                found = true;
                t = t2;
            }
            if (!found)
                return -1.0;

            return t*Math.Sqrt(dx*dx + dy*dy);
        }

        //calculate the point of intersection between two line segments
        public Point2D intersection(Line2D L, out bool found)
        {
            var pt = new Point2D(0.0, 0.0);
            var A = p1;
            var B = p2;
            var C = L.p1;
            var D = L.p2;

            var rTop = (A.y - C.y)*(D.x - C.x) - (A.x - C.x)*(D.y - C.y);
            var rBot = (B.x - A.x)*(D.y - C.y) - (B.y - A.y)*(D.x - C.x);

            var sTop = (A.y - C.y)*(B.x - A.x) - (A.x - C.x)*(B.y - A.y);
            var sBot = (B.x - A.x)*(D.y - C.y) - (B.y - A.y)*(D.x - C.x);

            if ((rBot == 0 || sBot == 0))
            {
                found = false;
                return pt;
            }
            var r = rTop/rBot;
            var s = sTop/sBot;
            if ((r > 0) && (r < 1) && (s > 0) && (s < 1))
            {
                pt.x = A.x + r*(B.x - A.x);
                pt.y = A.y + r*(B.y - A.y);
                found = true;
                return pt;
            }
            found = false;
            return pt;
        }

        //what is the squared distance from this line to a point 
        public double distance_sq(Point2D n)
        {
            var utop = (n.x - p1.x)*(p2.x - p1.x) + (n.y - p1.y)*(p2.y - p1.y);
            var ubot = p1.distance_sq(p2);
            var u = utop/ubot;

            if (u < 0 || u > 1)
            {
                var d1 = p1.distance_sq(n);
                var d2 = p2.distance_sq(n);
                if (d1 < d2) return d1;
                return d2;
            }
            var p = new Point2D(0.0, 0.0);
            p.x = p1.x + u*(p2.x - p1.x);
            p.y = p1.y + u*(p2.y - p1.y);
            return p.distance_sq(n);
        }

        //what is the distance from this line to a point
        public double distance(Point2D n)
        {
            return Math.Sqrt(distance_sq(n));
        }

        //what is the squared magnitude of this line segment
        public double length_sq()
        {
            return p1.distance_sq(p2);
        }

        //what is the length of this line segment
        public double length()
        {
            return p1.distance(p2);
        }
    }
}